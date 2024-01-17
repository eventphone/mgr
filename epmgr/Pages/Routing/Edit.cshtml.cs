using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data.ywsd;
using epmgr.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace epmgr.Pages.Routing
{
    public class EditModel : PageModel
    {
        private readonly YwsdDbContext _context;
        private readonly IServiceProvider _provider;

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [BindProperty]
        public Extension Extension { get; set; }

        public Data.ywsd.Yate[] Yates { get; private set; }

        public string Error { get; private set; }

        public EditModel(YwsdDbContext context, IServiceProvider provider)
        {
            _context = context;
            _provider = provider;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            await LoadYatesAsync(cancellationToken);
            if (Id is null) return Page();
            Extension = await _context.Extensions
                .AsNoTracking()
                .Where(x => x.Id == Id.Value)
                .Select(x => new Extension
                {
                    Id = x.Id,
                    Number = x.Number,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    Type = x.Type,
                    Language = x.Language,
                    Ringback = x.Ringback,
                    YateId = x.YateId,
                    ForwardingMode = x.ForwardingMode,
                    ForwardingDelay = x.ForwardingDelay,
                    ForwardingExtension = new Extension {Number = x.ForwardingExtension.Number},
                    OutgoingNumber = x.OutgoingNumber,
                    OutgoingName = x.OutgoingName,
                    IsDialoutAllowed = x.IsDialoutAllowed
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (Extension is null) return NotFound();
            return Page();
        }

        private async Task LoadYatesAsync(CancellationToken cancellationToken)
        {
            Yates = await _context.Yates
                .AsNoTracking()
                .Select(x => new Data.ywsd.Yate {Id = x.Id, Guru3Identifier = x.Guru3Identifier})
                .ToArrayAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (Extension is null) return RedirectToPage();
            Extension extension;
            if (Id is null)
            {
                extension = new Extension();
                _context.Extensions.Add(extension);
            }
            else
            {
                extension = await _context.Extensions
                    .Where(x=>x.Id == Id.Value)
                    .FirstAsync(cancellationToken);
            }
            extension.Number = Extension.Number;
            extension.Name = Extension.Name;
            extension.ShortName = Extension.ShortName;
            extension.Type = Extension.Type;
            extension.Language = Extension.Language;
            extension.Ringback = Extension.Ringback;
            extension.YateId = Extension.YateId;
            extension.ForwardingMode = Extension.ForwardingMode;
            extension.ForwardingDelay = Extension.ForwardingDelay;
            int? forwardingExtensionId = null;
            if (!String.IsNullOrEmpty(Extension.ForwardingExtension?.Number))
            {
                forwardingExtensionId = await _context.Extensions
                    .Where(x => x.Number == Extension.ForwardingExtension.Number)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            extension.ForwardingExtensionId = forwardingExtensionId;
            extension.OutgoingNumber = Extension.OutgoingNumber;
            extension.OutgoingName = Extension.OutgoingName;
            extension.IsDialoutAllowed = Extension.IsDialoutAllowed;
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (NpgsqlException ex)
            {
                Error = ex.Message;
                await LoadYatesAsync(cancellationToken);
                return Page();
            }
            catch (DbUpdateException ex) when(ex.InnerException is NpgsqlException)
            {
                Error = ex.InnerException.Message;
                await LoadYatesAsync(cancellationToken);
                return Page();
            }
            var services = _provider.GetServices<IYateService>();
            var tasks = services.Select(x => x.SetTrunking(extension.Number, extension.Type == ExtensionType.Trunk, cancellationToken));
            await Task.WhenAll(tasks);
            
            return RedirectToPage("Index", new {extension.Id});
        }

        public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
        {
            if (Id is null) return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var extension = await _context.Extensions.FirstOrDefaultAsync(x => x.Id == Id.Value, cancellationToken);
            if (extension is not null) 
            {
                _context.Extensions.Remove(extension);
                await _context.SaveChangesAsync(cancellationToken);
               await transaction.CommitAsync(cancellationToken);
            }

            return RedirectToPage("../Routing");
        }
    }
}