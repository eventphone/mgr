using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Hubs;
using epmgr.Model;
using epmgr.Omm;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using mitelapi;
using mitelapi.Events;
using mitelapi.Types;

namespace epmgr.Services
{
    public class RfpEventService : IHostedService
    {
        private readonly IOmmClient _client;
        private readonly IHubContext<MapHub> _mapHubContext;
        private readonly MapSettings _settings;

        public RfpEventService(IOmmClient client, IHubContext<MapHub> mapHubContext, IOptions<MapSettings> options)
        {
            _client = client;
            _mapHubContext = mapHubContext;
            _settings = options.Value;
            _client.RFPState += RFPStateChanged;
            _client.RFPSyncRel += RFPSyncChanged;
            _client.RFPCnf += RfpCnfChanged;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _client.SubscribeAsync(new[]
            {
                new SubscribeCmdType(EventType.RFPState) {RfpId = -1},
                new SubscribeCmdType(EventType.RFPSync) {RfpId = -1},
                new SubscribeCmdType(EventType.RFPCnf) {RfpId = -1},
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void RFPStateChanged(object sender, OmmEventArgs<EventRFPState> e)
        {
            var rfp = e.Event.rfp;
            if (rfp.SyncState.HasValue)
            {
                await _mapHubContext.Clients.All.SendAsync(nameof(MapHub.UpdateState), rfp.Id.Value, rfp.SyncState.Value.ToString());
            }
        }

        private async void RfpCnfChanged(object sender, OmmEventArgs<EventRFPCnf> e)
        {
            var rfp = e.Event.Rfp;
            if (rfp.X.HasValue || rfp.Y.HasValue)
            {
                var y = rfp.Y.GetValueOrDefault();
                var x = rfp.X.GetValueOrDefault();
                
                var lat = _settings.GetLatitude(y);
                var lng = _settings.GetLongitude(x);
                if (_mapHubContext.Clients?.All is null) return;
                await _mapHubContext.Clients.All.SendAsync(nameof(MapHub.UpdatePosition), rfp.Id.Value, lat, lng, rfp.Hierarchy2, x != 0 || y != 0);
            }
        }

        private async void RFPSyncChanged(object sender, OmmEventArgs<EventRFPSyncRel> e)
        {
            var syncs = e.Event.Forward
                .Where(x => !x.Lost)
                .Select(x => new RfpSyncMapModel
                {
                    From = e.Event.Id,
                    To = x.Id,
                    Rssi = -100 + x.Rssi,
                    Offset = x.Offset
                }).ToArray();
            await _mapHubContext.Clients.All.SendAsync(nameof(MapHub.UpdateSyncs), e.Event.Id, syncs);
        }
    }
}