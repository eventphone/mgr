using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data.ywsd;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace epmgr.Pages.Routing.ForkRank
{
    public class EditModel : PageModel
    {
        private readonly YwsdDbContext _context;

        [BindProperty(SupportsGet = true)]
        public int ExtensionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [BindProperty]
        public Data.ywsd.ForkRank ForkRank { get; set; }

        public string Error { get; private set; }

        public EditModel(YwsdDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            if (Id is null)
            {
                var index = await _context.ForkRanks
                    .Where(x => x.ExtensionId == ExtensionId)
                    .Select(x => (int?)x.Index)
                    .MaxAsync(cancellationToken);
                var extension = await _context.Extensions
                    .AsNoTracking()
                    .Where(x => x.Id == ExtensionId)
                    .Select(x => new Extension {Number = x.Number})
                    .FirstAsync(cancellationToken);
                ForkRank = new Data.ywsd.ForkRank
                {
                    Index = (index ?? -1) + 1,
                    Extension = extension
                };
                return Page();
            }
            ForkRank = await _context.ForkRanks
                .AsNoTracking()
                .Where(x => x.ExtensionId == ExtensionId)
                .Where(x => x.Id == Id.Value)
                .Select(x => new Data.ywsd.ForkRank
                {
                    Id = x.Id,
                    Index = x.Index,
                    Delay = x.Delay,
                    Mode = x.Mode,
                    Extension = new Extension {Number = x.Extension.Number}
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (ForkRank is null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (ForkRank is null) return RedirectToPage();
            Data.ywsd.ForkRank forkRank;
            if (Id is null)
            {
                forkRank = new Data.ywsd.ForkRank
                {
                    ExtensionId = ExtensionId
                };
                _context.ForkRanks.Add(forkRank);
            }
            else
            {
                forkRank = await _context.ForkRanks
                    .Where(x => x.Id == Id.Value)
                    .FirstAsync(cancellationToken);
            }
            forkRank.Index = ForkRank.Index;
            forkRank.Delay = ForkRank.Delay;
            forkRank.Mode = ForkRank.Mode;
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (NpgsqlException ex)
            {
                Error = ex.Message;
                return Page();
            }
            catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException)
            {
                Error = ex.Message;
                return Page();
            }
            return RedirectToPage("../ForkRank", new {forkRank.Id});
        }

        public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
        {
            if (Id is null) return NotFound();
            
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var forkRank = await _context.ForkRanks.FirstOrDefaultAsync(x => x.Id == Id.Value, cancellationToken);
            if (forkRank is not null) 
            {
                _context.ForkRanks.Remove(forkRank);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            
            return RedirectToPage("../Index", new {Id = ExtensionId});
        }
    }
}