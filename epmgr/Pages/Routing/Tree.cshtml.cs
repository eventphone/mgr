using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Ywsd;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace epmgr.Pages.Routing
{
    public class TreeModel : PageModel
    {
        private readonly YwsdClient _client;
        private readonly YwsdSettings _settings;

        public TreeModel(YwsdClient client, IOptions<YwsdSettings> options)
        {
            _client = client;
            _settings = options.Value;
        }

        [BindProperty(SupportsGet = true)]
        public string Caller { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Called { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Yate { get; set; }

        public YwsdRouting Routing { get; set; }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            if (!String.IsNullOrEmpty(Caller) && !String.IsNullOrEmpty(Called) && !String.IsNullOrEmpty(Yate))
            {
                var endpoint = GetYateEndpoint(Yate);
                Routing = await _client.GetRoutingTreeAsync(endpoint, Caller, Called, cancellationToken);
            }
        }

        private string GetYateEndpoint(string yateId)
        {
            switch (yateId)
            {
                case Data.ywsd.Yate.AppId:
                    return _settings.AppEndpoint;
                case Data.ywsd.Yate.DectId:
                    return _settings.DectEndpoint;
                case Data.ywsd.Yate.GsmId:
                    return _settings.GsmEndpoint;
                case Data.ywsd.Yate.PremiumId:
                    return _settings.PremiumEndpoint;
                case Data.ywsd.Yate.SipId:
                    return _settings.SipEndpoint;
                default:
                    throw new ArgumentOutOfRangeException(nameof(yateId));
            }
        }
    }
}