using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using mitelapi;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm
{
    public class RFPCaptureModel : PageModel
    {
        private readonly IOmmClient _ommClient;
        
        public RFPType[] Rfps { get; set; }
        
        public RFPCaptureModel(IOmmClient client)
        {
            _ommClient = client;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _ommClient.GetRFPCaptureListAsync(cancellationToken);
                Rfps = response.rfp;
            }
            catch (OmmNoEntryException)
            {
            }
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string mac, CancellationToken cancellationToken)
        {
            await _ommClient.DeleteRFPCaptureListElemAsync(mac, cancellationToken);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync(string mac, CancellationToken cancellationToken)
        {
            var rfp = new RFPType
            {
                EthAddr = mac,
                DectOn = true
            };
            var created = await _ommClient.CreateRFPAsync(rfp, cancellationToken);
            return RedirectToPage("/Omm/RFPs/Index", new { id = created.Id });
        }
    }
}