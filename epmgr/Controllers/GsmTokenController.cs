using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Gsm;
using epmgr.Model;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
	
namespace epmgr.Controllers
{
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class GsmTokenController : Controller
    {
        private readonly MgrDbContext _mgrContext;
        private readonly IGsmClient _gsmClient;
        private readonly Settings _settings;

        public GsmTokenController(MgrDbContext context, IGsmClient gsmClient, IOptions<Settings> options)
        {
            _mgrContext = context;
            _gsmClient = gsmClient;
            _settings = options.Value;
        }

        [HttpPost]
        public async Task<StatusCodeResult> BindUser([FromBody] BindUserModel model, CancellationToken cancellationToken)
        {
            if (!Request.Headers.ContainsKey("apikey"))
                return new UnauthorizedResult();
            if (Request.Headers["apikey"].All(x => x != _settings.ApiKey))
                return new UnauthorizedResult();
            if (!ModelState.IsValid)
                return new BadRequestResult();

            
            var extension = await _mgrContext.Extensions
                .Where(x => x.Token == model.Token)
                .Where(x => x.Type == MgrExtensionType.GSM)
                .FirstOrDefaultAsync(cancellationToken);
            if (extension == null || String.IsNullOrEmpty(extension.Extension))
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            
            var existing = await _gsmClient.GetSubscriberAsync(extension.Extension, cancellationToken);
            if (existing != null)
                return new StatusCodeResult((int)HttpStatusCode.Gone);
            existing = await _gsmClient.GetSubscriberAsync(model.Caller, cancellationToken);
            if (existing == null)
                return new BadRequestResult();

            cancellationToken = CancellationToken.None;//cancellation is no longer possible - we need to run till the end
            await _gsmClient.UpdateExtensionAsync(model.Caller, extension.Name, extension.Extension, cancellationToken);
            await _gsmClient.SetUmtsEnabledAsync(extension.Extension, extension.UseEncryption, cancellationToken);
            return new StatusCodeResult((int)HttpStatusCode.OK);
        }

        [HttpPost]
        public async Task<StatusCodeResult> Check([FromBody] BindUserModel model, CancellationToken cancellationToken)
        {
            if (!Request.Headers.ContainsKey("apikey"))
                return new UnauthorizedResult();
            if (Request.Headers["apikey"].All(x => x != _settings.ApiKey))
                return new UnauthorizedResult();
            if (!ModelState.IsValid)
                return new BadRequestResult();

            var tokenextension = await _mgrContext.Extensions
                .Where(x => x.Token == model.Token)
                .Where(x => x.Type == MgrExtensionType.GSM)
                .Select(x => x.Extension)
                .FirstOrDefaultAsync(cancellationToken);
            if (String.IsNullOrEmpty(tokenextension))
                return new StatusCodeResult((int)HttpStatusCode.NotFound);

            var exists = await _gsmClient.GetSubscriberAsync(tokenextension, cancellationToken);
            if (exists != null)
                return new StatusCodeResult((int)HttpStatusCode.Gone);

            return new StatusCodeResult((int)HttpStatusCode.OK);
        }
    }
}