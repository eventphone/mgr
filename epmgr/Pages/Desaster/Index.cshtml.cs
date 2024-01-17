using epmgr.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace epmgr.Pages.Desaster
{
    public class IndexModel : PageModel
    {
        private readonly MgrDbContext _context;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public MgrDesasterCall Desaster { get; set; }

        public IndexModel(MgrDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            Desaster = await _context.DesasterCalls
                .AsNoTracking()
                .Where(x => x.Id == Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (Desaster is null) return NotFound();
            return Page();

        }
    }
}
