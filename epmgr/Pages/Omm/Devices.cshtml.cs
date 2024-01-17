using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using mitelapi.Messages;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm
{
    public class DevicesModel : PageModel
    {
        private readonly IOmmClient _ommClient;

        public List<PPDevType> Devices { get; } = new List<PPDevType>();

        public GetPPStateResp[] States { get; private set; }

        [BindProperty(SupportsGet = true, Name = "q")]
        public string Query { get; set; }

        public DevicesModel(IOmmClient client)
        {
            _ommClient = client;
        }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            var all = await _ommClient.GetPPAllDevAsync(cancellationToken);
            if (!String.IsNullOrEmpty(Query))
            {
                Query = Query.ToLowerInvariant();
                foreach (var device in all)
                {
                    if (device.Uid.ToString().ToLowerInvariant().Contains(Query))
                    {
                        Devices.Add(device);
                    }
                    else if (device.Ppn.ToString().ToLowerInvariant().Contains(Query))
                    {
                        Devices.Add(device);
                    }
                    else if (device.HwType.ToLowerInvariant().Contains(Query))
                    {
                        Devices.Add(device);
                    }
                    else if (device.Ipei.Replace(" ", String.Empty).ToLowerInvariant().Contains(Query.Replace(" ", String.Empty)))
                    {
                        Devices.Add(device);
                    }
                }
            }
            else
            {
                Devices.AddRange(all.Where(x => x.Uid == 0));
            }
            var tasks = new List<Task<GetPPStateResp>>();
            foreach (var device in Devices)
            {
                tasks.Add(_ommClient.GetPPStateAsync(device.Ppn, cancellationToken));
            }
            States = await Task.WhenAll(tasks);
        }
    }
}