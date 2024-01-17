using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using epmgr.Services;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm.Devices
{
    public class IndexModel : PageModel
    {
        private readonly IOmmClient _ommClient;
        private readonly OmmSyncService _dectService;

        public PPDevType Device { get; set; }

        public IndexModel(IOmmClient client, OmmSyncService dectService)
        {
            _ommClient = client;
            _dectService = dectService;
        }

        public async Task OnGetAsync(int id, CancellationToken cancellationToken)
        {
            Device = await _ommClient.GetPPDevAsync(id, cancellationToken);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
        {
            await _ommClient.DeletePPDevAsync(id, cancellationToken);
            return RedirectToPage("../Devices");
        }

        public async Task<IActionResult> OnPostAddTempProfileAsync(int id, CancellationToken cancellationToken)
        {
            var pp = await _ommClient.GetPPDevAsync(id, cancellationToken);
            if (pp.Uid == 0)
            {
                var tmpUser = await _dectService.CreateTempUserProfile(CancellationToken.None);
                await _dectService.AttachDevice(tmpUser.Uid, id, CancellationToken.None);
            }
            return RedirectToPage("../Devices");
        }
    }
}