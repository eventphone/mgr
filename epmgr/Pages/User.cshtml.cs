using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Pages
{
    public class UserModel : PageModel
    {
        private readonly MgrDbContext _context;

        public MgrUser[] Users { get; set; }

        public UserModel(MgrDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            Users = await _context.Users.ToArrayAsync(cancellationToken);
        }
    }
}