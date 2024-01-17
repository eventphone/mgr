using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace epmgr.Services
{
    public class YateAppSyncService : YateSyncService<AppUser>
    {
        private readonly AppDbContext _context;
        private readonly GuruSettings _settings;

        public YateAppSyncService(ILogger<YateAppSyncService> logger, AppDbContext context, IOptions<GuruSettings> settings):base(logger)
        {
            _context = context;
            _settings = settings?.Value;
        }

        protected override IQueryable<AppUser> Users => _context.Users;

        protected override DbSet<AppUser> DbSet => _context.Users;

        protected override bool IsValidUpdate(MgrCreateExtension extension)
        {
            return extension.Type == MgrExtensionType.ANNOUNCEMENT ||
                extension.Type == MgrExtensionType.APP && !String.IsNullOrEmpty(extension.DirectRoutingTarget);
        }

        protected override AppUser CreateUser(MgrCreateExtension extension)
        {
            return _context.Users.Add(new AppUser{Username = extension.Number}).Entity;
        }

        protected override void UpdateUser(AppUser user, MgrCreateExtension extension)
        {
            user.Password = String.Empty;
            user.Type = "static";
            if (extension.Type == MgrExtensionType.ANNOUNCEMENT)
            {
                var path = _settings.AnnouncementFolder;
                if (path.StartsWith("/opt/sounds/"))
                    path = path.Substring(12);
                user.StaticTarget = $"external/nodata/playrandom.tcl {path}/{extension.AnnouncementAudio}.slin;accept_call=true";
            }
            else
            {
                user.StaticTarget = extension.DirectRoutingTarget;
            }
        }

        protected override Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}