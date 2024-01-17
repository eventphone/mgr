using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using Microsoft.AspNetCore.Mvc;
	
namespace epmgr.Controllers
{
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class RfpController : Controller
    {
        private readonly IOmmClient _ommClient;

        public RfpController(IOmmClient client)
        {
            _ommClient = client;
        }

         public async Task<IActionResult> By(string ip, CancellationToken cancellationToken) 
         {
            var rfps = await _ommClient.GetRFPAllAsync(false, true, cancellationToken);

            var rfp = rfps.Where(x => x.IpAddr == ip);
            return new JsonResult(rfp);
        }
    }
}