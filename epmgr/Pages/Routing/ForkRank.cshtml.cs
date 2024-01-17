using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data.ywsd;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Pages.Routing
{
    public class ForkRankModel : PageModel
    {
        private readonly YwsdDbContext _context;
        
        public Data.ywsd.ForkRank ForkRank { get; private set; }

        public ForkRankModel(YwsdDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
        {
            ForkRank = await _context.ForkRanks.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new Data.ywsd.ForkRank
                {
                    Id = x.Id,
                    Index = x.Index,
                    Mode = x.Mode,
                    Delay = x.Delay,
                    Extension = new Extension
                    {
                        Id = x.ExtensionId,
                        Number = x.Extension.Number
                    },
                    ForkRankMember = x.ForkRankMember.Select(m => new ForkRankMember
                    {
                        Extension = new Extension
                        {
                            Id = m.ExtensionId,
                            Number = m.Extension.Number
                        },
                        Type = m.Type,
                        IsActive = m.IsActive
                    }).ToArray()
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (ForkRank is null) return NotFound();
            return Page();
        }
    }
}