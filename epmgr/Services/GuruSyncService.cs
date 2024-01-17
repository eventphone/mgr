using epmgr.Data;
using epmgr.Guru;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using Hangfire;

namespace epmgr.Services
{
    public class GuruSyncService:BackgroundService
    {
        private readonly ILogger<GuruSyncService> _logger;
        private readonly GuruSettings _settings;
        private readonly GuruClient _guruClient;
        private readonly GuruWebsocket _websocket;
        private readonly IServiceProvider _serviceprovider;
        private readonly SemaphoreSlim _websocketTrigger = new SemaphoreSlim(0, 1);

        public GuruSyncService(ILoggerFactory loggerFactory, IOptions<GuruSettings> options, GuruClient guruClient, GuruWebsocket websocket, IServiceProvider serviceProvider)
        {
            _logger = loggerFactory?.CreateLogger<GuruSyncService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _settings = options.Value;
            _guruClient = guruClient;
            _websocket = websocket;
            _serviceprovider = serviceProvider;
            if (_websocket is {})
            {
                _websocket.NewMessage += WebsocketMessageNotify;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(_settings.Endpoint)) return;
            var wsTask = _websocket.RunWebsocketAsync(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SyncGuruMessageAsync(cancellationToken);
                }
                catch (TaskCanceledException ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError("Guru3 sync cancelled");
                        return;
                    }
                    _logger.LogError(ex, "Error while executing GuruSyncService");
                }
                catch (GuruSyncException ex)
                {
                    _logger.LogError(ex, "Error while processing message {0}", ex.GuruMessage.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while executing GuruSyncService");
                }
                var wsTrigger = _websocketTrigger.WaitAsync(TimeSpan.FromSeconds(_settings.Interval), cancellationToken);
                await Task.WhenAny(wsTrigger, wsTask);
            }
        }

        private void WebsocketMessageNotify(object sender, EventArgs e)
        {
            if (_websocketTrigger.CurrentCount == 0)
                _websocketTrigger.Release();
        }

        private Task ProcessMessageBatchAsync(IList<GuruMessage> messages, IEnumerable<IGuruMessageHandler> handlers, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queueing Batch({0})", messages.Count);
            var queue = GuruHandlerQueue.Create(handlers);
            return Task.WhenAll(messages.Select(x => queue.ProcessMessageAsync(x, cancellationToken)));
        }

        public IEnumerable<IList<GuruMessage>> CreateBatches(IEnumerable<GuruMessage> messages)
        {
            var result = new List<GuruMessage>();
            foreach (var message in messages)
            {
                switch (message.Type)
                {
                    case GuruMessageType.UpdateGroup:
                    case GuruMessageType.UpdateExtension:
                    case GuruMessageType.DeleteExtension:
                    case GuruMessageType.UnsubscribeDevice:
                    case GuruMessageType.RenameExtension:
                        result.Add(message);
                        break;
                    case GuruMessageType.EndResync:
                    case GuruMessageType.StartResync:
                        if (result.Count > 0)
                        {
                            yield return result;
                            result = new List<GuruMessage>();
                        }
                        yield return new[] {message};
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(message.Type), message.Type, "Unsupported GURU3 message type");
                }
            }
            if (result.Count > 0)
                yield return result;
        }

        private async Task SyncGuruMessageAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                GuruMessage[] messages;
                try
                {
                    messages = await _guruClient.GetMessagesAsync(cancellationToken);
                }
                catch (HttpRequestException)
                {
                    break;
                }
                if (messages.Length == 0) break;
                _logger.LogInformation($"Fetched {messages.Length} messages from guru");
                var batches = CreateBatches(messages);
                using (var scope = _serviceprovider.CreateScope())
                {
                    var handlers = scope.ServiceProvider.GetServices<IGuruMessageHandler>().ToList();
                    handlers.Add(new GuruAudioHandler(_settings));
                    var logger = scope.ServiceProvider.GetService<ILogger<GuruAckHandler>>();
                    handlers.Add(new GuruAckHandler(logger, _guruClient));
                    foreach(var batch in batches)
                    {
                        await ProcessMessageBatchAsync(batch, handlers, cancellationToken);
                    }
                }
            }
            using (var scope = _serviceprovider.CreateScope())
            {
                for (int i = 0; i < 50; i++)
                {
                    var context = scope.ServiceProvider.GetRequiredService<MgrDbContext>();
                    var messages = await context.MessageQueue
                        .AsNoTracking()
                        .Where(x => !x.Failed)
                        .OrderBy(x => x.Timestamp)
                        .Take(10)
                        .ToListAsync(cancellationToken);
                    if (messages.Count > 0)
                    {
                        var result = await _guruClient.SubmitMessageAsync(messages, cancellationToken);
                        var acked = result.Messages.Where(x => x.Status == "OK").Select(x => x.Id).ToArray();
                        var toDelete = await context.MessageQueue.Where(x => acked.Contains(x.Id)).ToListAsync(cancellationToken);
                        context.MessageQueue.RemoveRange(toDelete);
                        var naked = result.Messages.Where(x => x.Status != "OK").GroupBy(x => x.Text, x => x.Id);
                        foreach (var nak in naked)
                        {
                            var ids = nak.ToArray();
                            var dbMessages = await context.MessageQueue.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
                            foreach(var message in dbMessages)
                            {
                                message.Failed = true;
                                message.Error = nak.Key;
                            }
                        }
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        class GuruAudioHandler:IGuruMessageHandler
        {
            private readonly GuruSettings _settings;

            public int Order => 40;

            public bool IsThreadSafe => true;

            public GuruAudioHandler(GuruSettings settings)
            {
                _settings = settings;
            }

            public Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
            {
                if (!String.IsNullOrEmpty(guruMessage.Data?.RingbackTone))
                    BackgroundJob.Enqueue<GuruClient>(x => x.DownloadRingbackAsync(guruMessage.Data.RingbackTone, _settings.RingbackFolder, CancellationToken.None));
                if (!String.IsNullOrEmpty(guruMessage.Data?.AnnouncementAudio))
                    BackgroundJob.Enqueue<GuruClient>(x => x.DownloadAnnouncementAsync(guruMessage.Data.AnnouncementAudio, _settings.AnnouncementFolder, CancellationToken.None));
                return Task.CompletedTask;
            }
        }

        class GuruAckHandler : IGuruMessageHandler
        {
            private readonly ILogger<GuruAckHandler> _logger;
            private readonly GuruClient _client;

            public int Order => Int32.MaxValue;

            public bool IsThreadSafe => true;

            public GuruAckHandler(ILogger<GuruAckHandler> logger, GuruClient client)
            {
                _logger = logger;
                _client = client;
            }

            public Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Acking message '{0}'", guruMessage.Id);
                return _client.AcknowledgeMessageAsync(guruMessage, cancellationToken);
            }
        }
    }
}
