using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using epmgr.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using epmgr.Data.ywsd;

namespace epmgr.Services
{
    public class StatsService : BackgroundService
    {
        private readonly ILogger<StatsService> _logger;
        private readonly Settings _settings;
        private readonly IServiceProvider _serviceprovider;

        public StatsService(ILogger<StatsService> logger, IOptions<Settings> options, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settings = options.Value;
            _serviceprovider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(_settings.GraphiteHost))
                return;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await SendMetricsAsync(cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing StatsService");
                throw;
            }
        }


        private async Task SendMetricsAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceprovider.CreateScope())
            {
                try
                {
                    var sip = scope.ServiceProvider.GetService<SipDbContext>();
                    var premium = scope.ServiceProvider.GetService<PremiumDbContext>();
                    var gsm = scope.ServiceProvider.GetService<GsmDbContext>();
                    var app = scope.ServiceProvider.GetService<AppDbContext>();
                    var dect = scope.ServiceProvider.GetService<DectDbContext>();
                    var ywsd = scope.ServiceProvider.GetService<YwsdDbContext>();
                    var groups = ywsd.Extensions.Where(x => x.Type == ExtensionType.Group).CountAsync(cancellationToken);
                    var sipUsers = CountUsers(sip, cancellationToken);
                    var appUsers = CountUsers(app, cancellationToken);
                    var dectUsers = CountUsers(dect, cancellationToken);
                    var premiumUsers = CountUsers(premium, cancellationToken);
                    var gsmUsers = CountUsers(gsm, cancellationToken);
                    await Task.WhenAll(sipUsers, premiumUsers, gsmUsers, appUsers, dectUsers, groups);
                    var sipSubscribed = CountSubscribed(sip, cancellationToken);
                    var dectSubscribed = CountSubscribed(dect, cancellationToken);
                    var premiumSubscribed = CountSubscribed(premium , cancellationToken);
                    await Task.WhenAll(dectSubscribed, sipSubscribed, premiumSubscribed);
                    var sipUsed = CountUsed(sip, cancellationToken);
                    var dectUsed = CountUsed(dect, cancellationToken);
                    var premiumUsed = CountUsed(premium, cancellationToken);
                    var gsmUsed = CountUsed(gsm, cancellationToken);
                    await Task.WhenAll(sipUsed, dectUsed, premiumUsed, premiumUsed, gsmUsed);
                    var values = new Dictionary<string, double>
                    {
                        {"omm.mgr.users.sip", sipUsers.Result },
                        {"omm.mgr.users.premium", premiumUsers.Result },
                        {"omm.mgr.users.gsm", gsmUsers.Result },
                        {"omm.mgr.users.app", appUsers.Result },
                        {"omm.mgr.users.dect", dectUsers.Result },
                        {"omm.mgr.users.group", groups.Result },
                        {"omm.mgr.users.subscribed.sip", sipSubscribed.Result },
                        {"omm.mgr.users.subscribed.premium", premiumSubscribed.Result },
                        {"omm.mgr.users.subscribed.dect", dectSubscribed.Result },
                        {"omm.mgr.users.used.sip", sipUsed.Result },
                        {"omm.mgr.users.used.premium", premiumUsed.Result },
                        {"omm.mgr.users.used.dect", dectUsed.Result },
                        {"omm.mgr.users.used.gsm", gsmUsed.Result },
                    };
                    await SendMetricValuesAsync(values, cancellationToken);
                    _logger.LogDebug("Sent metrics");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while transmitting user stats");
                }
            }
        }

        private Task<long> CountUsers<T>(IYateDbContext<T> context, CancellationToken cancellationToken) where T : YateUser
        {
            if (context is null) return Task.FromResult(0L);
            return context.Users.LongCountAsync(cancellationToken);
        }

        private Task<long> CountSubscribed<T>(IYateDbContext<T> context, CancellationToken cancellationToken) where T : YateUser
        {
            if (context is null) return Task.FromResult(0L);
            return context.Registrations.Where(x => x.Location != null).LongCountAsync(cancellationToken);
        }

        private Task<long> CountUsed<T>(IYateDbContext<T> context, CancellationToken cancellationToken) where T : YateUser
        {
            if (context is null) return Task.FromResult(0L);
            return context.Users.Where(x => x.InUse != 0).LongCountAsync(cancellationToken);
        }

        public async Task SendMetricValuesAsync(IDictionary<string, double> values, CancellationToken cancellationToken)
        {
            using (var client = new TcpClient(AddressFamily.InterNetworkV6))
            {
                client.Client.DualMode = true;
                await client.ConnectAsync(_settings.GraphiteHost, _settings.GraphitePort, cancellationToken);
                using (var stream = client.GetStream())
                {
                    using (var writer = new StreamWriter(stream) {NewLine = "\n"})
                    {
                        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                        foreach (var datapoint in values)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await writer.WriteLineAsync($"{datapoint.Key} {datapoint.Value.ToString(CultureInfo.InvariantCulture)} {timestamp}");
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        await writer.FlushAsync();
                    }
                }
            }
        }
    }
}