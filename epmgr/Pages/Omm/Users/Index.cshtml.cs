using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm.Users
{
    public class IndexModel : PageModel
    {
        private readonly IOmmClient _ommClient;

        public PPUserType UserModel { get; set; }

        public IndexModel(IOmmClient client)
        {
            _ommClient = client;
        }

        public async Task OnGetAsync(int id, CancellationToken cancellationToken)
        {
            UserModel = await _ommClient.GetPPUserAsync(id, cancellationToken);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
        {
            await _ommClient.DeletePPUserAsync(id, cancellationToken);
            return RedirectToPage("../Users");
        }
    }
}