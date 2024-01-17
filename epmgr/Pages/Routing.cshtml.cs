using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data.ywsd;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Pages
{
    public class RoutingModel : PageModel
    {
        private readonly YwsdDbContext _context;

        [BindProperty(Name = "q", SupportsGet = true)]
        public string Query { get; set; }

        public List<Extension> Users { get; set; }

        public RoutingModel(YwsdDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            var q = Query?.ToLower();
            if (String.IsNullOrEmpty(q))
                return;

            Users = await _context.Extensions
                .AsNoTracking()
                .Where(x => x.Number.ToLower().Contains(q) || x.Name.ToLower().Contains(q))
                .Select(x => new Extension
                {
                    Id = x.Id,
                    Number = x.Number,
                    Name = x.Name,
                    Language = x.Language,
                    Type = x.Type,
                    Yate = new Data.ywsd.Yate {Hostname = x.Yate.Hostname},
                })
                .OrderBy(x => x.Number)
                .ToListAsync(cancellationToken);
        }
    }
}