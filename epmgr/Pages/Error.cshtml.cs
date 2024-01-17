using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages
{
    public class ErrorModel : PageModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !String.IsNullOrEmpty(RequestId);

        public string Message { get; set; }

        public void OnGet(int statusCode)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            switch (statusCode)
            {
                case 500:
                    Message = "The number you have dialed is temporarily not available.";
                    break;
                case 404:
                    Message = "The number you have dialed is not in service.";
                    break;
                default:
                    var reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
                    throw new ArgumentException($"Unexpected Statuscode {statusCode} in {reExecute.OriginalPath}");
            }
        }
    }
}
