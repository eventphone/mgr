using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace epmgr.Services
{
    public class YateDectSyncService : YateSyncService<DectUser>
    {
        private readonly DectDbContext _context;

        public YateDectSyncService(ILogger<YateDectSyncService> logger, DectDbContext context) : base(logger)
        {
            _context = context;
        }

        protected override IQueryable<DectUser> Users => _context.Users;

        protected override DbSet<DectUser> DbSet => _context.Users;

        protected override bool IsValidUpdate(MgrCreateExtension extension)
        {
            return extension.Type == MgrExtensionType.DECT;
        }

        protected override DectUser CreateUser(MgrCreateExtension extension)
        {
            return _context.Users.Add(new DectUser {Username = extension.Number}).Entity;
        }

        protected override void UpdateUser(DectUser user, MgrCreateExtension extension)
        {
            user.Password = extension.Password;
            user.DisplayMode = extension.DisplayModus;
        }

        protected override Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}