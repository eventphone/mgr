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
    public class YateGsmSyncService : YateSyncService<GsmUser>
    {
        private readonly GsmDbContext _context;
        private GsmSettings _options;

        public YateGsmSyncService(ILogger<YateGsmSyncService> logger, GsmDbContext context, IOptions<GsmSettings> options) : base(logger)
        {
            _context = context;
            _options = options?.Value;
        }

        protected override IQueryable<GsmUser> Users => _context.Users;

        protected override DbSet<GsmUser> DbSet => _context.Users;  

        protected override bool IsValidUpdate(MgrCreateExtension extension)
        {
            return extension.Type == MgrExtensionType.GSM;
        }

        protected override GsmUser CreateUser(MgrCreateExtension extension)
        {
            return _context.Users.Add(new GsmUser {Username = extension.Number}).Entity;
        }

        protected override void UpdateUser(GsmUser user, MgrCreateExtension extension)
        {
            user.Password = extension.Password;
            user.Type = "static";
            user.StaticTarget = $"sip/sip:{extension.Number}@{_options.SipServer};oconnection_id=gsm";
        }

        protected override Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}