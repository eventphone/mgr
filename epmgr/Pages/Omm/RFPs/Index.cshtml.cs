using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm.RFPs
{
    public class IndexModel : PageModel
    {
        private readonly IOmmClient _ommClient;

        [BindProperty]
        public RFPType RFP { get; set; }

        public IndexModel(IOmmClient client)
        {
            _ommClient = client;
        }

        public async Task OnGetAsync(int id, CancellationToken cancellationToken)
        {
            RFP = await _ommClient.GetRFPAsync(id, true, true, cancellationToken);
        }

        public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
        {
            if (ModelState.TryGetValue($"{nameof(RFP)}.{nameof(RFP.Hierarchy1)}", out var value))
            {
                RFP.Hierarchy1 = value.AttemptedValue;
            }
            if (ModelState.TryGetValue($"{nameof(RFP)}.{nameof(RFP.Hierarchy2)}", out value))
            {
                RFP.Hierarchy2 = value.AttemptedValue;
            }
            if (ModelState.TryGetValue($"{nameof(RFP)}.{nameof(RFP.Hierarchy3)}", out value))
            {
                RFP.Hierarchy3 = value.AttemptedValue;
            }
            if (ModelState.TryGetValue($"{nameof(RFP)}.{nameof(RFP.Hierarchy4)}", out value))
            {
                RFP.Hierarchy4 = value.AttemptedValue;
            }
            await _ommClient.SetRFPAsync(RFP, cancellationToken);
            return RedirectToPage("../RFPs");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
        {
            await _ommClient.DeleteRFPAsync(id, cancellationToken);
            return RedirectToPage("../RFPs");
        }

        public async Task<IActionResult> OnPostReenrollAsync(int id, CancellationToken cancellationToken)
        {
            await _ommClient.RequestRFPEnrollmentAsync((uint) id, cancellationToken);
            return RedirectToPage("../RFPs");
        }
    }
}