using System.Runtime;
using System.Threading.Tasks;
using epmgr.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using mitelapi.Types;

namespace epmgr.Hubs
{
    [Authorize]
    public class MapHub : Hub
    {
        public Task UpdatePosition(int id, double lat, double lng, string level)
        {
            if (Clients?.Others is null) return Task.CompletedTask;
            return Clients.Others.SendAsync(nameof(UpdatePosition), id, lat, lng, level, true);
        }

        public Task UpdateState(int id, RFPSyncStateType state)
        {
            if (Clients?.All is null) return Task.CompletedTask;
            return Clients.All.SendAsync(nameof(UpdateState), id, state.ToString());
        }

        public Task UpdateSyncs(int id, RfpSyncMapModel[] syncs)
        {
            if (syncs != null && Clients?.All != null)
            {
                return Clients.All.SendAsync(nameof(UpdateSyncs), id, syncs);
            }
            return Task.CompletedTask;
        }
    }
}