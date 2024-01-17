using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data.ywsd;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Pages.Routing
{
    public class IndexModel : PageModel
    {
        private readonly YwsdDbContext _context;

        public Extension Extension { get; private set; }

        public IndexModel(YwsdDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
        {
            Extension = await _context.Extensions.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new Extension
                {
                    Id = x.Id,
                    Name = x.Name,
                    Number = x.Number,
                    Language = x.Language,
                    Type = x.Type,
                    Yate = new Data.ywsd.Yate { Hostname = x.Yate.Hostname },
                    Ringback = x.Ringback,
                    ForwardingMode = x.ForwardingMode,
                    ForwardingDelay = x.ForwardingDelay,
                    ForwardingExtension = x.ForwardingExtensionId != null ? new Extension
                    {
                        Id = x.ForwardingExtension.Id,
                        Number = x.ForwardingExtension.Number
                    } : null,
                    ForkRanks = x.ForkRanks.Select(f => new Data.ywsd.ForkRank
                    {
                        Id = f.Id,
                        Index = f.Index,
                        Mode = f.Mode,
                        Delay = f.Delay
                    }).ToArray(),
                    OutgoingName = x.OutgoingName,
                    OutgoingNumber = x.OutgoingNumber,
                    IsDialoutAllowed = x.IsDialoutAllowed,
                    ShortName = x.ShortName
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (Extension is null) return NotFound();

            return Page();
        }
    }
}