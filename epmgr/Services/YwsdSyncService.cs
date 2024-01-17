using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Data.ywsd;
using epmgr.Guru;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace epmgr.Services
{
    public class YwsdSyncService : IGuruMessageHandler, IExtensionService
    {
        private readonly ILogger<YwsdSyncService> _logger;
        private readonly YwsdDbContext _context;

        public int Order => 10;

        public bool IsThreadSafe => false;

        public YwsdSyncService(ILogger<YwsdSyncService> logger, YwsdDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
        {
            switch (guruMessage.Type)
            {
                case GuruMessageType.UpdateExtension:
                    return CreateOrUpdateExtension(MgrCreateExtension.Create(guruMessage.Data), cancellationToken);
                case GuruMessageType.UpdateGroup:
                    return UpdateForkRanksAsync(guruMessage.Data, cancellationToken);
                case GuruMessageType.DeleteExtension:
                    return DeleteExtension(guruMessage.Data.Number, cancellationToken);
                case GuruMessageType.RenameExtension:
                    return RenameExtension(guruMessage.Data.OldExtension, guruMessage.Data.NewExtension, cancellationToken);
                case GuruMessageType.StartResync:
                case GuruMessageType.EndResync:
                case GuruMessageType.UnsubscribeDevice:
                    return Task.CompletedTask;
                default:
                    throw new ArgumentOutOfRangeException(nameof(guruMessage.Type), guruMessage.Type, "Unsupported GURU3 message type");
            }
        }

        private async Task RenameExtension(string oldExtension, string newExtension, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Renaming '{0}' to '{1}'", oldExtension, newExtension);
            var dbExtension = await _context.Extensions
                .Where(x => x.Number == oldExtension)
                .FirstOrDefaultAsync(cancellationToken);
            if (dbExtension is not null) 
            {
                dbExtension.Number = newExtension;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task DeleteExtension(string extension, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting '{0}'", extension);
            var dbExtension = await _context.Extensions.Where(x => x.Number == extension).FirstOrDefaultAsync(cancellationToken);
            if (dbExtension is not null) 
            {
                _context.Extensions.Remove(dbExtension);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task UpdateForkRanksAsync(GuruData guruData, CancellationToken cancellationToken)
        {
            var extension = await _context.Extensions
                .Where(x => x.Number == guruData.Number)
                .FirstAsync(cancellationToken);
            if (extension.Type == ExtensionType.Simple)
            {
                if (guruData.Extensions.Length > 0)
                {
                    extension.Type = ExtensionType.Multiring;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            else if (extension.Type == ExtensionType.Multiring)
            {
                if (guruData.Extensions.Length == 0)
                {
                    extension.Type = ExtensionType.Simple;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            else if (extension.Type != ExtensionType.Group)
            {
                throw new NotSupportedException("UPDATE_GROUP is only supported for Group or Multiring.");
            }

            var guruForkRanks = guruData.Extensions
                .GroupBy(x => x.Delay.GetValueOrDefault(0))
                .OrderBy(x => x.Key)
                .Select((x,i)=>new {Index = i, Extension = x})
                .ToList();
            
            var existingForkRanks = await _context.ForkRanks
                .Where(x => x.Extension.Number == guruData.Number)
                .OrderBy(x => x.Index)
                .ToListAsync(cancellationToken);
            var previousDelay = 0;
            foreach (var forkRank in existingForkRanks)
            {
                var guruForkRank = guruForkRanks.FirstOrDefault(x => x.Index == forkRank.Index);
                if (guruForkRank is null)
                {
                    _logger.LogInformation("Deleting forkrank '{0}' from group '{1}'", forkRank.Index, guruData.Number);
                    _context.ForkRanks.Remove(forkRank);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    guruForkRanks.Remove(guruForkRank);
                    forkRank.Mode = guruForkRank.Extension.Key == 0 ? ForkRankMode.Default : ForkRankMode.Next;
                    forkRank.Delay = (guruForkRank.Extension.Key - previousDelay) * 1000;
                    previousDelay = guruForkRank.Extension.Key;
                    await _context.SaveChangesAsync(cancellationToken);
                    await UpdateForkRankMemberAsync(forkRank, guruForkRank.Extension, cancellationToken);
                }
            }
            if (guruForkRanks.Any())
            {
                var extensionId = extension.Id;
                foreach (var missing in guruForkRanks)
                {
                    var forkRank = new ForkRank
                    {
                        ExtensionId = extensionId,
                        Index = missing.Index,
                        Mode = missing.Extension.Key == 0 ? ForkRankMode.Default : ForkRankMode.Next,
                        Delay = (missing.Extension.Key - previousDelay) * 1000
                    };
                    previousDelay = missing.Extension.Key;
                    _logger.LogInformation("Adding forkrank '{0}' for group '{1}'", forkRank.Index, guruData.Number);
                    _context.ForkRanks.Add(forkRank);
                    await _context.SaveChangesAsync(cancellationToken);
                    await UpdateForkRankMemberAsync(forkRank, missing.Extension, cancellationToken);
                }
            }
        }

        private async Task UpdateForkRankMemberAsync(ForkRank forkRank, IEnumerable<GuruExtension> guruMembers, CancellationToken cancellationToken)
        {
            
            var existingMembers = await _context.ForkRanks
                .Where(x=>x.Id == forkRank.Id)
                .SelectMany(x => x.ForkRankMember)
                .Select(x=>new {ForkRankMember = x, x.Extension.Number})
                .ToListAsync(cancellationToken);
            var members = guruMembers.ToList();
            foreach (var existing in existingMembers)
            {
                var guruMember = members.FirstOrDefault(x => x.Extension == existing.Number);
                if (guruMember is null)
                {
                    _logger.LogInformation("Deleting member '{0}' from forkRank '{1}'", existing.Number, forkRank.Index);
                    _context.ForkRankMember.Remove(existing.ForkRankMember);
                }
                else
                {
                    existing.ForkRankMember.IsActive = guruMember.Active;
                    members.Remove(guruMember);
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
            if (members.Any())
            {
                foreach (var missing in members)
                {
                    var extension = await _context.Extensions
                        .Where(x => x.Number == missing.Extension)
                        .Select(x => new{x.Id})
                        .FirstOrDefaultAsync(cancellationToken);

                    if (extension is null)
                        throw new InvalidOperationException($"Referenced extension {missing.Extension} not found.");

                    _logger.LogInformation("Adding member '{0}' to forkRank '{1}'", missing.Extension, forkRank.Index);
                    _context.ForkRankMember.Add(new ForkRankMember
                    {
                        ForkRankId = forkRank.Id,
                        ExtensionId = extension.Id,
                        IsActive = missing.Active,
                        Type = ForkRankMemberType.Default
                    });
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public Task CreateExtension(MgrCreateExtension extension, CancellationToken cancellationToken)
        {
            return CreateOrUpdateExtension(extension, cancellationToken);
        }

        public async Task CreateOrUpdateExtension(MgrCreateExtension extension, CancellationToken cancellationToken)
        {
            if (extension.Type == MgrExtensionType.SPECIAL) return;
            var yateId = await GetYateIdAsync(extension.Type, cancellationToken);
            var extensionType = GetExtensionType(extension);
            if ((extensionType == ExtensionType.Simple || extensionType == ExtensionType.Multiring || extensionType == ExtensionType.Trunk) && yateId is null)
            {
                throw new InvalidOperationException($"Yate type {extension.Type} not found for extension {extension.Number}");
            }
            var existing = await _context.Extensions
                .Where(x => x.Number == extension.Number)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing == null || existing.Type != extensionType)
            {
                if (existing != null)
                {
                    _logger.LogInformation("Deleting extension '{1}' before recreate", existing.Number);
                    _context.Extensions.Remove(existing);
                }
                existing = new Extension
                {
                    Number = extension.Number,
                    ForwardingMode = ForwardingMode.Disabled,
                    Type = extensionType
                };
                _logger.LogInformation("Creating extension '{1}'", existing.Number);
                _context.Extensions.Add(existing);
            }

            existing.Name = extension.Name;
            if (existing.Type == ExtensionType.Group)
            {
                if (String.IsNullOrEmpty(extension.ShortName))
                    existing.ShortName = "GRP";
                else
                    existing.ShortName = extension.ShortName;
            }
            existing.Ringback = extension.RingbackTone;
            existing.IsDialoutAllowed = extension.AllowDialout;
            existing.Language = extension.Language;
            existing.YateId = yateId;
            existing.ForwardingMode = extension.ForwardingMode;
            existing.ForwardingDelay = extension.ForwardingDelay * 1000;
            if (!String.IsNullOrEmpty(extension.ForwardingExtension))
            {
                var forwarding = await _context.Extensions
                    .Where(x => x.Number == extension.ForwardingExtension)
                    .Select(x => new{x.Id})
                    .FirstOrDefaultAsync(cancellationToken);
                if (forwarding is null)
                    throw new InvalidOperationException($"Referenced extension {extension.ForwardingExtension} not found.");
                existing.ForwardingExtensionId = forwarding.Id;
            }
            else
            {
                existing.ForwardingExtensionId = null;
            }
            _logger.LogInformation("Updating extension '{1}'", existing.Number);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private ExtensionType GetExtensionType(MgrCreateExtension extension)
        {
            if (extension.IsTrunk) return ExtensionType.Trunk;
            if (extension.Type == MgrExtensionType.GROUP) return ExtensionType.Group;
            if (extension.HasMultiring) return ExtensionType.Multiring;
            return ExtensionType.Simple;
        }

        private Task<int?> GetYateIdAsync(MgrExtensionType type, CancellationToken cancellationToken)
        {
            string guruIdentifier;
            switch (type)
            {
                case MgrExtensionType.DECT:
                    guruIdentifier = Yate.DectId;
                    break;
                case MgrExtensionType.SIP:
                    guruIdentifier = Yate.SipId;
                    break;
                case MgrExtensionType.PREMIUM:
                    guruIdentifier = Yate.PremiumId;
                    break;
                case MgrExtensionType.GSM:
                    guruIdentifier = Yate.GsmId;
                    break;
                case MgrExtensionType.APP:
                case MgrExtensionType.ANNOUNCEMENT:
                    guruIdentifier = Yate.AppId;
                    break;
                case MgrExtensionType.GROUP:
                case MgrExtensionType.SPECIAL:
                    return Task.FromResult<int?>(null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported GURU3 extension type");
            }
            return _context.Yates
                .Where(x => x.Guru3Identifier == guruIdentifier)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}