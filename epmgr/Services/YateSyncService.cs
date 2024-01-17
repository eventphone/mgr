using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace epmgr.Services
{
    public abstract class YateSyncService<T> : IGuruMessageHandler, IExtensionService, IYateService where T:YateUser,new()
    {
        protected readonly ILogger Logger;

        public int Order => 20;

        public bool IsThreadSafe => false;

        protected YateSyncService(ILogger logger)
        {
            Logger = logger;
        }

        public Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
        {
            switch (guruMessage.Type)
            {
                case GuruMessageType.UpdateExtension:
                    return CreateExtension(MgrCreateExtension.Create(guruMessage.Data), cancellationToken);
                case GuruMessageType.DeleteExtension:
                    return DeleteExtension(guruMessage.Data.Number, cancellationToken);
                case GuruMessageType.RenameExtension:
                    return RenameExtension(guruMessage.Data.OldExtension, guruMessage.Data.NewExtension, cancellationToken);
                case GuruMessageType.UpdateGroup:
                case GuruMessageType.EndResync:
                case GuruMessageType.StartResync:
                case GuruMessageType.UnsubscribeDevice:
                    return Task.CompletedTask;
                default:
                    throw new ArgumentOutOfRangeException(nameof(guruMessage.Type), guruMessage.Type, "Unsupported GURU3 message type");
            }
        }

        private async Task RenameExtension(string oldExtension, string newExtension, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Renaming '{0}' to '{1}'", oldExtension, newExtension);
            var existing = await Users
                .Where(x => x.Username == oldExtension)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null) return;

            await DeleteExtension(oldExtension, cancellationToken);
            
            existing.Username = newExtension;
            DbSet.Add(existing);
            await SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteExtension(string extension, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Deleting '{0}'", extension);
            var user = await Users.Where(x => x.Username == extension).FirstOrDefaultAsync(cancellationToken);
            if (user is not null) 
            {
                DbSet.Remove(user);
                await SaveChangesAsync(cancellationToken);
            }
        }

        public Task CreateExtension(MgrCreateExtension extension, CancellationToken cancellationToken)
        {
            if (extension.Type == MgrExtensionType.SPECIAL) return Task.CompletedTask;
            if (IsValidUpdate(extension))
            {
                return CreateOrUpdateUserAsync(extension, cancellationToken);
            }
            else
            {
                return DeleteExtension(extension.Number, cancellationToken);
            }
        }

        private async Task CreateOrUpdateUserAsync(MgrCreateExtension extension, CancellationToken cancellationToken)
        {
            var existing = await Users.Where(x => x.Username == extension.Number).FirstOrDefaultAsync(cancellationToken);
            if (existing is null)
            {
                Logger.LogInformation("Creating extension '{1}'", extension.Number);
                existing = CreateUser(extension);
            }
            existing.Type = "user";
            existing.DisplayName = extension.Name;
            existing.CallWaiting = extension.CallWaiting;
            existing.IsTrunk = extension.IsTrunk;
            UpdateUser(existing, extension);
            Logger.LogInformation("Updating extension '{1}'", existing.Username);
            await SaveChangesAsync(cancellationToken);
        }

        protected abstract IQueryable<T> Users { get; }

        protected abstract DbSet<T> DbSet { get; }

        protected abstract bool IsValidUpdate(MgrCreateExtension extension);

        protected abstract T CreateUser(MgrCreateExtension extension);

        protected abstract void UpdateUser(T user, MgrCreateExtension extension);

        protected abstract Task SaveChangesAsync(CancellationToken cancellationToken);

        public async Task<IEnumerable<YateUser>> QueryAsync(string query, CancellationToken cancellationToken)
        {
            query = query.ToLower();
            return await Users.Where(x => x.Username.ToLower().Contains(query) || x.DisplayName.ToLower().Contains(query)).ToListAsync(cancellationToken);
        }

        public async Task SetTrunking(string extension, bool isTrunk, CancellationToken cancellationToken)
        {
            var existing = await Users.Where(x => x.Username == extension).FirstOrDefaultAsync(cancellationToken);
            if (existing is null) return;
            existing.IsTrunk = isTrunk;
            await SaveChangesAsync(cancellationToken);
        }

        public Task<T> GetUserAsync(string username, CancellationToken cancellationToken)
        {
            return GetUserQuery(username).FirstOrDefaultAsync(cancellationToken);
        }

        public IQueryable<T> GetUserQuery(string username)
        {
            return Users.Where(x => x.Username == username);
        }
    }
}