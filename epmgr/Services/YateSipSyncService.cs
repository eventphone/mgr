using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace epmgr.Services
{
    public class YateSipSyncService : YateSyncService<SipUser>
    {
        private readonly SipDbContext _context;

        public YateSipSyncService(ILogger<YateSipSyncService> logger, SipDbContext context) : base(logger)
        {
            _context = context;
        }

        protected override IQueryable<SipUser> Users => _context.Users;

        protected override DbSet<SipUser> DbSet => _context.Users;

        protected override bool IsValidUpdate(MgrCreateExtension extension)
        {
            return extension.Type == MgrExtensionType.SIP;
        }

        protected override SipUser CreateUser(MgrCreateExtension extension)
        {
            return _context.Users.Add(new SipUser {Username = extension.Number}).Entity;
        }

        protected override void UpdateUser(SipUser user, MgrCreateExtension extension)
        {
            user.Password = extension.Password;
        }

        protected override Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}