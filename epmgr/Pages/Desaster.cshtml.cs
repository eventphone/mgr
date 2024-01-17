using epmgr.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace epmgr.Pages
{
    public class DesasterModel : PageModel
    {
        private readonly MgrDbContext _context;

        public ICollection<MgrDesasterCall> DesasterCalls { get; set; }

        public DesasterModel(MgrDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            DesasterCalls = await _context.DesasterCalls
                .OrderBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync();
            return Page();
        }
    }
}
