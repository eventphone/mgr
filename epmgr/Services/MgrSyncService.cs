using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace epmgr.Services
{
    public class MgrSyncService:IGuruMessageHandler
    {
        private readonly ILogger<MgrSyncService> _logger;
        private readonly MgrDbContext _context;
        private readonly IServiceProvider _provider;
        private readonly RandomPasswordService _password;

        public int Order => 0;

        public bool IsThreadSafe => false;

        public MgrSyncService(ILogger<MgrSyncService> logger, MgrDbContext context, IServiceProvider provider, RandomPasswordService password)
        {
            _logger = logger;
            _context = context;
            _provider = provider;
            _password = password;
        }

        public Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
        {
            switch (guruMessage.Type)
            {
                case GuruMessageType.StartResync:
                    _logger.LogInformation("Starting resync");
                    return _context.Database.ExecuteSqlRawAsync("UPDATE \"Extension\" SET \"DeleteAfterResync\" = true", cancellationToken);
                case GuruMessageType.EndResync:
                    return DeleteAfterResyncAsync(cancellationToken);
                case GuruMessageType.UpdateExtension:
                    return UpdateExtensionAsync(guruMessage.Data, cancellationToken);
                case GuruMessageType.DeleteExtension:
                    return DeleteExtensionAsync(guruMessage.Data.Number, cancellationToken);
                case GuruMessageType.RenameExtension:
                    return RenameExtension(guruMessage.Data.OldExtension, guruMessage.Data.NewExtension, cancellationToken);
                case GuruMessageType.UpdateGroup:
                case GuruMessageType.UnsubscribeDevice:
                    return Task.CompletedTask;
                default:
                    throw new ArgumentOutOfRangeException(nameof(guruMessage.Type), guruMessage.Type, "Unsupported GURU3 message type");
            }
        }

        private async Task RenameExtension(string oldExtension, string newExtension, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Renaming '{0}' to '{1}'", oldExtension, newExtension);
            var extension = await _context.Extensions
                .Where(x => x.Extension == oldExtension)
                .FirstOrDefaultAsync(cancellationToken);
            if (extension is not null) 
            {
                extension.Extension = newExtension;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task DeleteExtensionAsync(string extension, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting '{0}'", extension);
            var dbExtension = await _context.Extensions.Where(x => x.Extension == extension).FirstOrDefaultAsync(cancellationToken);
            if (dbExtension is not null) 
            {
                _context.Extensions.Remove(dbExtension);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task UpdateExtensionAsync(GuruData guruData, CancellationToken cancellationToken)
        {
            var existing = await _context.Extensions
                .Where(x => x.Extension == guruData.Number)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing == null)
            {
                existing = new MgrExtension{Extension = guruData.Number, Password = _password.GenerateSipPassword()};
                _logger.LogInformation("Creating extension '{1}'", existing.Extension);
                _context.Extensions.Add(existing);
            }
            existing.Name = guruData.Name;
            existing.DeleteAfterResync = false;
            existing.UseEncryption = guruData.UseEncryption.GetValueOrDefault();
            existing.Token = guruData.Token;
            existing.Language = guruData.Language;
            existing.Type = guruData.Type;
            if (guruData.Type == MgrExtensionType.DECT || guruData.Type == MgrExtensionType.GSM)
            {
                if (String.IsNullOrEmpty(existing.Password))
                {
                    //extension might be changed from APP or Announcement
                    existing.Password = _password.GenerateSipPassword();
                }
                //use generated password
                guruData.Password = existing.Password;
            }
            else
            {
                existing.Password = guruData.Password;
            }
            _logger.LogInformation("Updating extension '{1}'", existing.Extension);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task DeleteAfterResyncAsync(CancellationToken cancellationToken)
        {
            var extensions = await _context.Extensions
                .Where(x => x.DeleteAfterResync)
                .Select(x => x.Extension)
                .ToListAsync(cancellationToken);
            var extensionServices = _provider.GetService<IEnumerable<IExtensionService>>()?.ToList();
            
            _logger.LogInformation("Cleaning up after resync");
            //TODO prevent deletion of temp extensions from dect and gsm
            foreach (var extension in extensions)
            {
                if (!(extensionServices is null))
                {
                    var tasks = extensionServices.Select(x => x.DeleteExtension(extension, cancellationToken));

                    await Task.WhenAll(tasks);
                }
                await DeleteExtensionAsync(extension, cancellationToken);
            }
            _logger.LogInformation("Finished resync");
        }
    }
}