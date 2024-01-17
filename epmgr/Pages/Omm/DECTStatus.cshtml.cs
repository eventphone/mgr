using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using epmgr.Services;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm
{
    public class DECTStatusModel : PageModel
    {
        private readonly IOmmClient _ommClient;
        private readonly OmmEventService _service;

        [BindProperty]
        public bool RFPCapture { get; set; }

        [BindProperty]
        public bool AutoCreate { get; set; }

        [BindProperty]
        public DECTSubscriptionModeType Mode { get; set; }

        public DECTStatusModel(IOmmClient client, OmmEventService service)
        {
            _ommClient = client;
            _service = service;
        }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            Mode = await _ommClient.GetDECTSubscriptionModeAsync(cancellationToken);
            AutoCreate = await _ommClient.GetDevAutoCreateAsync(cancellationToken);
            RFPCapture = await _ommClient.GetRFPCaptureAsync(cancellationToken);
        }

        public async Task<IActionResult> OnPostAutocreateAsync(CancellationToken cancellationToken)
        {
            await _ommClient.SetDevAutoCreateAsync(AutoCreate, cancellationToken);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSubscriptionModeAsync(CancellationToken cancellationToken)
        {
            _service.AllowDectSubscription = Mode != DECTSubscriptionModeType.Off;
            await _ommClient.SetDECTSubscriptionModeAsync(Mode, cancellationToken);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRFPCaptureAsync(CancellationToken cancellationToken)
        {
            await _ommClient.SetRFPCaptureAsync(RFPCapture, cancellationToken);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCleanup(CancellationToken cancellationToken)
        {
            var users = await _ommClient.GetPPAllUserAsync(cancellationToken);
            foreach (var user in users)
            {
                await _ommClient.DeletePPUserAsync(user.Uid, cancellationToken);
            }

            var devices = await _ommClient.GetPPAllDevAsync(cancellationToken);
            foreach (var device in devices)
            {
                await _ommClient.DeletePPDevAsync(device.Ppn, cancellationToken);
            }
            return RedirectToPage();
        }
    }
}