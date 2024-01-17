using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using epmgr.Omm;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mitelapi;
using mitelapi.Events;
using mitelapi.Types;

namespace epmgr.Services
{
    public class OmmEventService:IHostedService
    {
        private readonly IOmmClient _client;
        private readonly ILogger<IOmmClient> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Regex[] _ipeiBlacklist;
        private readonly OmmSettings _settings;

        public OmmEventService(IOmmClient client, ILogger<IOmmClient> logger, IOptions<OmmSettings> options, IServiceProvider serviceProvider)
        {
            _client = client;
            _logger = logger;
            _settings = options.Value;
            _serviceProvider = serviceProvider;
            _client.MessageLog += LogOmmMessage;
            _ipeiBlacklist = _settings.IPEIBlacklist.Select(x => new Regex(x)).ToArray();
        }

        private void LogOmmMessage(object sender, LogMessageEventArgs e)
        {
            var prefix = e.Direction == MessageDirection.In ? "<OMM: " : ">OMM:";
            _logger.LogDebug(prefix + e.Message);
        }

        public bool AllowDectSubscription { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            AllowDectSubscription = true;
            await _client.LoginAsync(_settings.User, _settings.Password, userDeviceSync:true, cancellationToken);
            var cleanupTask = CleanupUsers(cancellationToken);
            var enableSubscription = _client.SetDECTSubscriptionModeAsync(DECTSubscriptionModeType.Configured, cancellationToken);
            var enableRfpCapture = _client.SetRFPCaptureAsync(true, cancellationToken);
            var enableAutocreate = _client.SetDevAutoCreateAsync(true, cancellationToken);
            await _client.SubscribeAsync(new[]
            {
                new SubscribeCmdType(EventType.PPDevCnf) {Ppn = -1},
                new SubscribeCmdType(EventType.DECTSubscriptionMode),
                new SubscribeCmdType(EventType.PPUserCnf) { Uid = -1 }
            }, cancellationToken);
            _client.PPCnf += DeviceAdded;
            _client.DECTSubscriptionModeChanged += DECTSubscriptionModeChanged;
            await cleanupTask;
            await CreateMissingTempUser(cancellationToken);
            await enableSubscription;
            await enableRfpCapture;
            await enableAutocreate;
        }

        private async Task CreateMissingTempUser(CancellationToken cancellationToken)
        {
            var ppn = 0;
            while (true)
            {
                try
                {
                    var devices = await _client.GetPPDevAsync(ppn, 20, cancellationToken);
                    ppn = devices.Max(x => x.Ppn) + 1;
                    foreach (var device in devices)
                    {
                        if (device.Uid == 0)
                        {
                            await CreateTempUserAsync(device, cancellationToken);
                        }
                    }
                }
                catch (OmmNoEntryException)
                {
                    break;
                }
            }
        }

        private async Task CleanupUsers(CancellationToken cancellationToken)
        {
            var users = await _client.GetPPAllUserAsync(cancellationToken);
            var tasks = new List<Task>();
            foreach (var user in users.Where(x=>x.Ppn == 0))
            {
                tasks.Add(_client.DeletePPUserAsync(user.Uid, cancellationToken));
            }
            await Task.WhenAll(tasks);
        }

        private async void DeviceAdded(object sender, OmmEventArgs<EventPPCnf> e)
        {
            var device = e.Event.Device;
            if (device == null)
            {
                return;
            }
            try
            {
                if (IsBlacklisted(device))
                {
                    await _client.DeletePPDevAsync(device.Ppn, CancellationToken.None);
                }
                else if (device.AutoCreate.GetValueOrDefault())
                {
                    await CreateTempUserAsync(device, CancellationToken.None);
                }
                else if (e.Event.User?.PpnOld == device.Ppn && e.Event.User.Ppn == 0)
                {
                    //reregistered already registered device
                    await ReattachDevice(e.Event.User, CancellationToken.None);
                }
                if (device.Uak != null)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var pp = await _client.GetPPDevAsync(device.Ppn, CancellationToken.None);
                        var encryption = false;
                        if (pp.Uid.HasValue && pp.Uid.Value != 0)
                        {
                            var user = await _client.GetPPUserAsync(pp.Uid.Value, CancellationToken.None);
                            if (!user.Num.StartsWith(Settings.DectTempPrefix))
                            {
                                var mgrContext = scope.ServiceProvider.GetRequiredService<MgrDbContext>();
                                var mgrExtension = mgrContext.Extensions
                                    .Where(x => x.Extension == user.Num)
                                    .Select(x => new { x.UseEncryption })
                                    .FirstOrDefault();
                                if (mgrExtension != null)
                                {
                                    encryption = mgrExtension.UseEncryption;
                                    var message = new GuruData
                                    {
                                        Ipei = pp.Ipei,
                                        Uak = pp.Uak,
                                        Extension = user.Num
                                    };
                                    mgrContext.MessageQueue.Add(new MgrMessage
                                    {
                                        Type = GuruMessageType.AssignHandset,
                                        Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                                        Json = message.Serialize()
                                    });
                                    await mgrContext.SaveChangesAsync();
                                }
                            }
                        }
                        var dectService = scope.ServiceProvider.GetRequiredService<OmmSyncService>();
                        await dectService.SetEncryption(device.Ppn, encryption, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing DeviceAdded");
            }
        }

        private async void DECTSubscriptionModeChanged(object sender, OmmEventArgs<EventDECTSubscriptionMode> e)
        {
            if (AllowDectSubscription && e.Event.Mode == DECTSubscriptionModeType.Off)
            {
                await _client.SetDECTSubscriptionModeAsync(DECTSubscriptionModeType.Configured, CancellationToken.None);
            }
        }

        private async Task CreateTempUserAsync(PPDevType device, CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dectService = scope.ServiceProvider.GetRequiredService<OmmSyncService>();
                var tmpUser = await dectService.CreateTempUserProfile(cancellationToken);
                await dectService.AttachDevice(tmpUser.Uid, device.Ppn, cancellationToken);

                BackgroundJob.Enqueue<YateService>(x => x.PlayWave("AN020_de-DE", tmpUser.Num, "9955", "DECT BASE", CancellationToken.None));
            }
        }

        private async Task ReattachDevice(PPUserType user, CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dectService = scope.ServiceProvider.GetRequiredService<OmmSyncService>();
                if (user.Num == null)
                {
                    user = await _client.GetPPUserAsync(user.Uid, cancellationToken);
                }

                await dectService.AttachDevice(user.Uid, user.PpnOld.Value, cancellationToken);

                var mgrContext = scope.ServiceProvider.GetRequiredService<MgrDbContext>();
                var encryption = await mgrContext.Extensions.Where(x => x.Extension == user.Num)
                    .Select(x => x.UseEncryption).FirstOrDefaultAsync(cancellationToken);

                BackgroundJob.Schedule<OmmSyncService>(x => x.SetEncryption(user.PpnOld.Value, encryption, CancellationToken.None), TimeSpan.FromSeconds(30));

                //todo still needed? do we have a race somewhere? it should have worked before...
                if (!user.Num.StartsWith(Settings.DectTempPrefix))
                {
                    var pp = await _client.GetPPDevAsync(user.PpnOld.Value, cancellationToken);
                    var message = new GuruData
                    {
                        Ipei = pp.Ipei,
                        Uak = pp.Uak,
                        Extension = user.Num
                    };
                    mgrContext.MessageQueue.Add(new MgrMessage
                    {
                        Type = GuruMessageType.AssignHandset,
                        Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        Json = message.Serialize()
                    });
                    await mgrContext.SaveChangesAsync(cancellationToken);
                }

                BackgroundJob.Enqueue<YateService>(x => x.PlayWave("AN020_de-DE", user.Num, "9955", "DECT BASE", CancellationToken.None));
            }
        }

        private bool IsBlacklisted(PPDevType device)
        {
            if (device.Ipei != null && _ipeiBlacklist.Any(x => x.IsMatch(device.Ipei)))
            {
                return true;
            }
            return false;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}