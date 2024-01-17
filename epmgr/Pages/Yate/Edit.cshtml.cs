using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace epmgr.Pages.Yate
{
    public class EditModel : PageModel
    {
        private readonly IServiceProvider _serviceProvider;
        
        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }
        
        public YateUser Extension { get; set; }

        [BindProperty]
        public DectDisplayModus DisplayMode { get; set; }

        public string Error { get; private set; }

        public EditModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IActionResult OnGet()
        {
            Extension = new AppUser{InUse = 0};
            return Page();
        }

        public IActionResult OnPost()
        {
            return NotFound();
        }

        public Task<IActionResult> OnGetAppAsync(CancellationToken cancellationToken)
        {
            return GetAsync(_serviceProvider.GetRequiredService<AppDbContext>().Users, cancellationToken);
        }

        public Task<IActionResult> OnGetAnnouncementAsync(CancellationToken cancellationToken)
        {
            return GetAsync(_serviceProvider.GetRequiredService<AppDbContext>().Users, cancellationToken);
        }

        public async Task<IActionResult> OnGetDectAsync(CancellationToken cancellationToken)
        {
            var result = await GetAsync(_serviceProvider.GetRequiredService<DectDbContext>().Users, cancellationToken);
            DisplayMode = ((DectUser) Extension).DisplayMode;
            return result;
        }

        public Task<IActionResult> OnGetGsmAsync(CancellationToken cancellationToken)
        {
            return GetAsync(_serviceProvider.GetRequiredService<GsmDbContext>().Users, cancellationToken);
        }

        public Task<IActionResult> OnGetPremiumAsync(CancellationToken cancellationToken)
        {
            return GetAsync(_serviceProvider.GetRequiredService<PremiumDbContext>().Users, cancellationToken);
        }

        public Task<IActionResult> OnGetSipAsync(CancellationToken cancellationToken)
        {
            return GetAsync(_serviceProvider.GetRequiredService<SipDbContext>().Users, cancellationToken);
        }

        public Task<IActionResult> OnPostAppAsync(AppUser extension, CancellationToken cancellationToken)
        {
            extension.Password = String.Empty;
            return OnPostAsync<AppUser, AppDbContext>(extension, cancellationToken);
        }

        public Task<IActionResult> OnPostAnnouncementAsync(AppUser extension, CancellationToken cancellationToken)
        {
            extension.Password = String.Empty;
            return OnPostAsync<AppUser, AppDbContext>(extension, cancellationToken);
        }

        public async Task<IActionResult> OnPostDectAsync(DectUser extension, CancellationToken cancellationToken)
        {
            if (extension is null) return RedirectToPage();
            Extension = extension;
            var context = _serviceProvider.GetRequiredService<DectDbContext>();
            DectUser existing = await GetUser(context.Users, extension, cancellationToken);
            CopyProperties(existing);
            extension.DisplayMode = DisplayMode;
            return await SaveChanges(context, cancellationToken);
        }

        public Task<IActionResult> OnPostGsmAsync(GsmUser extension, CancellationToken cancellationToken)
        {
            return OnPostAsync<GsmUser, GsmDbContext>(extension, cancellationToken);
        }

        public Task<IActionResult> OnPostPremiumAsync(PremiumUser extension, CancellationToken cancellationToken)
        {
            return OnPostAsync<PremiumUser, PremiumDbContext>(extension, cancellationToken);
        }

        public Task<IActionResult> OnPostSipAsync(SipUser extension, CancellationToken cancellationToken)
        {
            return OnPostAsync<SipUser, SipDbContext>(extension, cancellationToken);
        }

        public async Task<IActionResult> OnPostDeleteAsync(MgrExtensionType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case MgrExtensionType.DECT:
                    await OnPostDeleteAsync<DectUser, DectDbContext>(cancellationToken);
                    break;
                case MgrExtensionType.SIP:
                    await OnPostDeleteAsync<SipUser, SipDbContext>(cancellationToken);
                    break;
                case MgrExtensionType.PREMIUM:
                    await OnPostDeleteAsync<PremiumUser, PremiumDbContext>(cancellationToken);
                    break;
                case MgrExtensionType.GSM:
                    await OnPostDeleteAsync<GsmUser, GsmDbContext>(cancellationToken);
                    break;
                case MgrExtensionType.ANNOUNCEMENT:
                case MgrExtensionType.APP:
                    await OnPostDeleteAsync<AppUser, AppDbContext>(cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            return RedirectToPage("../Yate");
        }

        private async Task OnPostDeleteAsync<TUser, TDbContext>(CancellationToken cancellationToken)
            where TDbContext:DbContext, IYateDbContext<TUser> where TUser : YateUser, new()
        {
            var context = _serviceProvider.GetRequiredService<TDbContext>();
            context.Users.Remove(new TUser {Username = Id});
            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task<IActionResult> OnPostAsync<TUser, TDbContext>(TUser user, CancellationToken cancellationToken)
            where TDbContext:DbContext, IYateDbContext<TUser> where TUser : YateUser, new()
        {
            if (user is null) return RedirectToPage();
            Extension = user;
            var context = _serviceProvider.GetRequiredService<TDbContext>();
            TUser extension = await GetUser(context.Users, user, cancellationToken);
            CopyProperties(extension);
            return await SaveChanges(context, cancellationToken);
        }

        private async Task<IActionResult> SaveChanges(DbContext context, CancellationToken cancellationToken)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (NpgsqlException ex)
            {
                Error = ex.Message;
                return Page();
            }
            catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException)
            {
                Error = ex.InnerException.Message;
                return Page();
            }
            return RedirectToPage("Index", new {Type = Extension.UserType, Id = Extension.Username});
        }

        private async Task<T> GetUser<T>(DbSet<T> set, T user, CancellationToken cancellationToken) where T:YateUser, new()
        {
            if (Id is null)
            {
                var result = new T {Username = user.Username};
                set.Add(result);
                return result;
            }
            else
            {
                return await set
                    .Where(x => x.Username == Id)
                    .FirstAsync(cancellationToken);
            }
        }

        private void CopyProperties(YateUser user)
        {
            user.Username = Extension.Username;
            user.DisplayName = Extension.DisplayName;
            user.Password = Extension.Password;
            user.InUse = Extension.InUse;
            user.Type = Extension.Type;
            user.IsTrunk = Extension.IsTrunk;
            user.CallWaiting = Extension.CallWaiting;
            user.StaticTarget = Extension.StaticTarget;
        }

        private async Task<IActionResult> GetAsync(IQueryable<YateUser> query, CancellationToken cancellationToken)
        {
            if (Id is null) return NotFound();
            Extension = await query.AsNoTracking()
                .Where(x => x.Username == Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (Extension is null) return NotFound();
            return Page();
        }
    }
}