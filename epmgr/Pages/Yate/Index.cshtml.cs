using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace epmgr.Pages.Yate
{
    public class IndexModel : PageModel
    {
        private readonly IServiceProvider _serviceProvider;

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public MgrExtensionType Type { get; set; }

        public YateUser Extension { get; set; }

        public ICollection<YateRegistration> Registrations { get; set; }

        public IndexModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            Extension = await GetQuery()
                .AsNoTracking()
                .Where(x => x.Username == Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (Extension is null) return NotFound();
            Registrations = await GetRegistrationQuery()
                .AsNoTracking()
                .Where(x => x.Username == Id)
                .ToListAsync(cancellationToken);
            return Page();
        }
        
        private IQueryable<YateUser> GetQuery()
        {
            switch (Type)
            {
                case MgrExtensionType.DECT:
                    return _serviceProvider.GetRequiredService<DectDbContext>().Users;
                case MgrExtensionType.SIP:
                    return _serviceProvider.GetRequiredService<SipDbContext>().Users;
                case MgrExtensionType.PREMIUM:
                    return _serviceProvider.GetRequiredService<PremiumDbContext>().Users;
                case MgrExtensionType.GROUP:
                case MgrExtensionType.SPECIAL:
                    return Array.Empty<YateUser>().AsQueryable();
                case MgrExtensionType.GSM:
                    return _serviceProvider.GetRequiredService<GsmDbContext>().Users;
                case MgrExtensionType.ANNOUNCEMENT:
                case MgrExtensionType.APP:
                    return _serviceProvider.GetRequiredService<AppDbContext>().Users;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Type));
            }
        }
        
        private IQueryable<YateRegistration> GetRegistrationQuery()
        {
            switch (Type)
            {
                case MgrExtensionType.DECT:
                    return _serviceProvider.GetRequiredService<DectDbContext>().Registrations;
                case MgrExtensionType.SIP:
                    return _serviceProvider.GetRequiredService<SipDbContext>().Registrations;
                case MgrExtensionType.PREMIUM:
                    return _serviceProvider.GetRequiredService<PremiumDbContext>().Registrations;
                case MgrExtensionType.GROUP:
                case MgrExtensionType.SPECIAL:
                    return Array.Empty<YateRegistration>().AsQueryable();
                case MgrExtensionType.GSM:
                    return _serviceProvider.GetRequiredService<GsmDbContext>().Registrations;
                case MgrExtensionType.ANNOUNCEMENT:
                case MgrExtensionType.APP:
                    return _serviceProvider.GetRequiredService<AppDbContext>().Registrations;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Type));
            }
        }
    }
}