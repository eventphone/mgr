using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Hubs;
using epmgr.Model;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace epmgr.Services
{
    public class YateStatusService
    {
        private readonly ConnectionMultiplexer _redis;
        private long _clients = 0;
        private readonly ISubscriber _subscriber;
        private readonly IHubContext<StatusHub> _hub;

        public YateStatusService(ConnectionMultiplexer redis, IHubContext<StatusHub> hubContext)
        {
            _redis = redis;
            _subscriber = _redis.GetSubscriber();
            _hub = hubContext;
        }

        public Task ConnectAsync()
        {
            if (Interlocked.Increment(ref _clients) == 1)
                return SubscribeAsync();
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            if (Interlocked.Decrement(ref _clients) == 0)
                return _subscriber.UnsubscribeAllAsync(CommandFlags.FireAndForget);
            return Task.CompletedTask;
        }

        public async Task GetAllAsync(string connectionId, CancellationToken cancellationToken)
        {
            var endpoints = _redis.GetEndPoints();
            var db = _redis.GetDatabase();
            var client = _hub.Clients.Client(connectionId);
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = server.KeysAsync(pattern: "yate:ystatus:channel:*").WithCancellation(cancellationToken);
                await foreach (var key in keys)
                {
                    var data = await db.HashGetAllAsync(key);
                    var parts = key.ToString().Split(':');
                    await client.SendAsync("Update", new YateChannel(parts[4], parts[3], data), cancellationToken);
                }
                
                keys = server.KeysAsync(pattern: "yate:ystatus:message:*").WithCancellation(cancellationToken);
                await foreach (var key in keys)
                {
                    var data = await db.HashGetAllAsync(key);
                    var parts = key.ToString().Split(':');
                    await client.SendAsync("Alarm", new YateMessage(parts[4], parts[3], data), cancellationToken);
                }
            }
        }

        private Task SubscribeAsync()
        {
            var channel = "__keyspace@0__:yate:ystatus:*";
            return _subscriber.SubscribeAsync(new RedisChannel(channel, RedisChannel.PatternMode.Pattern), RedisNotify);
        }

        private async void RedisNotify(RedisChannel channel, RedisValue type)
        {
            var key = GetKey(channel);
            var parts = key.Split(':');
            switch (type)
            {
                case "del":
                case "expired":
                case "evicted":
                    await _hub.Clients.All.SendAsync("Delete", parts[4], parts[3]);
                    break;
                case "hset":
                    var redis = _redis.GetDatabase();
                    var data = await redis.HashGetAllAsync(key);
                    if (parts[2] == "channel")
                        await _hub.Clients.All.SendAsync("Update", new YateChannel(parts[4], parts[3], data));
                    else if (parts[2] == "message")
                        await _hub.Clients.All.SendAsync("Alarm", new YateMessage(parts[4], parts[3], data));
                    break;
            }
        }

        private static string GetKey(string channel)
        {
            var index = channel.IndexOf(':');
            if (index >= 0 && index < channel.Length - 1)
                return channel.Substring(index + 1);

            //we didn't find the delimiter, so just return the whole thing
            return channel;
        }
    }
}