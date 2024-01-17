using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Pages.User
{
    public class IndexModel : PageModel
    {
        private readonly MgrDbContext _context;

        public IndexModel(MgrDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is not null) 
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return RedirectToPage("../User");
        }
    }
}