using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Guru;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace epmgr.Services
{
    public class GuruWebsocket
    {
        private readonly GuruSettings _settings;
        private readonly ILogger<GuruWebsocket> _logger;
        private int _queueLength = 0;

        public GuruWebsocket(ILoggerFactory loggerFactory, IOptions<GuruSettings> options)
        {
            _settings = options.Value;
            _logger = loggerFactory?.CreateLogger<GuruWebsocket>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public event EventHandler<EventArgs> NewMessage;

        public async Task RunWebsocketAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(_settings.Endpoint)) return;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var socket = new ClientWebSocket())
                    {
                        socket.Options.SetRequestHeader("ApiKey", _settings.ApiKey);
                        var uri = new UriBuilder(_settings.Endpoint)
                        {
                            Scheme = "wss",
                            Path = "/status/stream/"
                        };
                        await socket.ConnectAsync(uri.Uri, cancellationToken);
                        await ReadMessagesAsync(socket, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GuruWebsocket");
                }
            }
        }

        private async Task ReadMessagesAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[512];
            while (!cancellationToken.IsCancellationRequested)
            {
                var remaining = buffer.AsMemory();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var read = await socket.ReceiveAsync(remaining, cancellationToken);
                    remaining = remaining.Slice(read.Count);
                    if (read.EndOfMessage) break; //complete message
                    if (remaining.Length < 128)
                    {
                        //resize buffer
                        var newBuffer = new byte[buffer.Length * 2];
                        buffer.AsSpan().CopyTo(newBuffer);
                        remaining = newBuffer.AsMemory(buffer.Length - remaining.Length);
                        buffer = newBuffer;
                    }
                }
                ProcessMessage(buffer.AsMemory(0, buffer.Length - remaining.Length));
            }
        }

        private void ProcessMessage(Memory<byte> message)
        {
            if (message.IsEmpty) return;
            var text = Encoding.UTF8.GetString(message.Span);
            var guruMessage = JsonConvert.DeserializeObject<GuruWsMessage>(text);
            if (guruMessage.Length > _queueLength)
            {
                OnNewMessage();
            }
            _queueLength = guruMessage.Length;
        }

        protected virtual void OnNewMessage()
        {
            NewMessage?.Invoke(this, EventArgs.Empty);
        }
    }
}