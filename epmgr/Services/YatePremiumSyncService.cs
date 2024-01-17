using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace epmgr.Services
{
    public class YatePremiumSyncService : YateSyncService<PremiumUser>
    {
        private readonly PremiumDbContext _context;

        public YatePremiumSyncService(ILogger<YatePremiumSyncService> logger, PremiumDbContext context) : base(logger)
        {
            _context = context;
        }

        protected override IQueryable<PremiumUser> Users => _context.Users;

        protected override DbSet<PremiumUser> DbSet => _context.Users;

        protected override bool IsValidUpdate(MgrCreateExtension extension)
        {
            return extension.Type == MgrExtensionType.PREMIUM;
        }

        protected override PremiumUser CreateUser(MgrCreateExtension extension)
        {
            return _context.Users.Add(new PremiumUser {Username = extension.Number}).Entity;
        }

        protected override void UpdateUser(PremiumUser user, MgrCreateExtension extension)
        {
            user.Password = extension.Password;
        }

        protected override Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}