using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Gsm;
using epmgr.Guru;

namespace epmgr.Services
{
    public class GsmSyncService:IGuruMessageHandler,IExtensionService
    {
        private readonly IGsmClient _client;

        public int Order => 40;

        public bool IsThreadSafe => true;

        public GsmSyncService(IGsmClient client)
        {
            _client = client;
        }

        public Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
        {
            switch (guruMessage.Type)
            {
                case GuruMessageType.DeleteExtension:
                    return DeleteExtension(guruMessage.Data.Number, cancellationToken);
                case GuruMessageType.UpdateExtension:
                    return CreateExtension(MgrCreateExtension.Create(guruMessage.Data), cancellationToken);
                case GuruMessageType.RenameExtension:
                    return RenameExtension(guruMessage.Data.OldExtension, guruMessage.Data.NewExtension, cancellationToken);
                case GuruMessageType.UnsubscribeDevice:
                    return DeleteExtension(guruMessage.Data.Extension, cancellationToken);
                case GuruMessageType.StartResync:
                case GuruMessageType.EndResync:
                case GuruMessageType.UpdateGroup:
                    return Task.CompletedTask;
                default:
                    throw new ArgumentOutOfRangeException(nameof(guruMessage.Type), guruMessage.Type, "Unsupported GURU3 message type");
            }
        }

        private async Task RenameExtension(string oldExtension, string newExtension, CancellationToken cancellationToken)
        {
            var subscriber = await _client.GetSubscriberAsync(oldExtension, cancellationToken);
            if (subscriber is null) return;
            await _client.UpdateExtensionAsync(oldExtension, null, newExtension, cancellationToken);
        }

        public async Task CreateExtension(MgrCreateExtension extension, CancellationToken cancellationToken)
        {
            if (extension.Type != MgrExtensionType.GSM)
            {
                await DeleteExtension(extension.Number, cancellationToken);
                return;
            }
            var subscriber = await _client.GetSubscriberAsync(extension.Number, cancellationToken);
            if (subscriber is null) return;
            await _client.SetUmtsEnabledAsync(subscriber.Number, extension.Use3G, cancellationToken);
        }

        public async Task DeleteExtension(string extension, CancellationToken cancellationToken)
        {
            var subscriber = await _client.GetSubscriberAsync(extension, cancellationToken);
            if (subscriber is null) return;
            await _client.ResetExtensionAsync(subscriber.Number, cancellationToken);
        }
    }
}