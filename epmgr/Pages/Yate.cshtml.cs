using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace epmgr.Pages
{
    public class YateModel : PageModel
    {
        private readonly IServiceProvider _provider;

        [BindProperty(Name = "q", SupportsGet = true)]
        public string Query { get; set; }

        public YateUser[] Users { get; set; }

        public YateModel(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var q = Query;
            if (String.IsNullOrEmpty(q))
                return Page();

            var services = _provider.GetServices<IYateService>();
            var tasks = services.Select(x => x.QueryAsync(q, cancellationToken)).ToList();

            var users = await Task.WhenAll(tasks);

            Users = users.SelectMany(x => x).OrderBy(x=>x.Username).ToArray();
            return Page();
        }

        public IActionResult OnPost(string id, string caller, string name, string file)
        {
            BackgroundJob.Enqueue<YateService>(x => x.PlayWave(file, id, caller, name, CancellationToken.None));
            return RedirectToPage(new {q = Query});
        }
    }
}