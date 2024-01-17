using epmgr.Data;
using epmgr.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace epmgr.Pages.Desaster
{
    public class EditModel : PageModel
    {
        private readonly MgrDbContext _context;
        private readonly RandomPasswordService _random;

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [BindProperty]
        public MgrDesasterCall Desaster { get; set; }

        public EditModel(MgrDbContext context, RandomPasswordService random)
        {
            _context = context;
            _random = random;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            if (Id is null) return Page();

            Desaster = await _context.DesasterCalls
                .AsNoTracking()
                .Where(x => x.Id == Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (Desaster is null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return Page();

            MgrDesasterCall desaster;
            if (Id is null)
            {
                desaster = new MgrDesasterCall();
                _context.DesasterCalls.Add(desaster);
                while (true)
                {
                    var pin = _random.GenerateDesasterPin();
                    var inUse = await _context.DesasterCalls
                        .Where(x => x.Pin == pin)
                        .AnyAsync(cancellationToken);
                    if (inUse) continue;
                    desaster.Pin = pin;
                    break;
                }
            }
            else
            {
                desaster = await _context.DesasterCalls
                    .Where(x => x.Id == Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (desaster is null) return NotFound();
            }
            desaster.Name = Desaster.Name;
            desaster.Announcement = Desaster.Announcement;
            desaster.Target = Desaster.Target;

            await _context.SaveChangesAsync(cancellationToken);
            return RedirectToPage("Index", new { desaster.Id });
        }

        public async Task<IActionResult> OnPostStartAsync(CancellationToken cancellationToken)
        {
            if (Id is null) return NotFound();
            var desaster = await _context.DesasterCalls
                .AsNoTracking()
                .Where(x => x.Id == Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (desaster is null) return NotFound();
            BackgroundJob.Enqueue<DesasterCaller>(x=>x.GenerateCallsAsync(desaster.Name, desaster.Announcement, desaster.Target, CancellationToken.None));
            return RedirectToPage("/Desaster");
        }

        public async Task<IActionResult> OnPostRegenerateAsync(CancellationToken cancellationToken)
        {
            if (Id is null) return NotFound();
            var existing = await _context.DesasterCalls
                .Where(x=>x.Id == Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null) return NotFound();
            while (true)
            {
                var pin = _random.GenerateDesasterPin();
                if (pin == existing.Pin) continue;
                var inUse = await _context.DesasterCalls
                    .Where(x => x.Pin == pin)
                    .AnyAsync(cancellationToken);
                if (inUse) continue;
                existing.Pin = pin;
                await _context.SaveChangesAsync(cancellationToken);
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
        {
            if (Id is null) return NotFound();

            var desaster = await _context.DesasterCalls
                .Where(x => x.Id == Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (desaster is not null)
            {
                _context.DesasterCalls.Remove(desaster);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return RedirectToPage("../Desaster");
        }
    }
}
