using System;
using System.Threading.Tasks;
using epmgr.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace epmgr.Hubs
{
    [Authorize]
    public class StatusHub : Hub
    {
        private readonly YateStatusService _status;

        public StatusHub(YateStatusService status)
        {
            _status = status;
        }

        public override Task OnConnectedAsync()
        {
            return _status.ConnectAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return _status.DisconnectAsync();
        }

        public Task GetAll()
        {
            return _status.GetAllAsync(Context.ConnectionId, Context.ConnectionAborted);
        }
    }
}