using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Guru;
using epmgr.Model;
using epmgr.Omm;
using epmgr.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using mitelapi;
using mitelapi.Types;

namespace epmgr.Controllers
{
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class DectTokenController : Controller
    {
        private readonly IOmmClient _ommClient;
        private readonly MgrDbContext _mgrContext;
        private readonly Settings _settings;
        private readonly OmmSyncService _dectService;

        public DectTokenController(IOmmClient client, MgrDbContext context, OmmSyncService dectService, IOptions<Settings> options)
        {
            _ommClient = client;
            _mgrContext = context;
            _settings = options.Value;
            _dectService = dectService;
        }

        [HttpPost]
        public async Task<StatusCodeResult> BindUser([FromBody] BindUserModel model, CancellationToken cancellationToken)
        {
            if (!Request.Headers.ContainsKey("apikey"))
                return new UnauthorizedResult();
            if (Request.Headers["apikey"].All(x => x != _settings.ApiKey))
                return new UnauthorizedResult();
            if (!ModelState.IsValid)
                return BadRequest();

            var extension = await _mgrContext.Extensions
                .Where(x => x.Token == model.Token)
                .Where(x => x.Type == MgrExtensionType.DECT)
                .FirstOrDefaultAsync(cancellationToken);
            if (extension == null || String.IsNullOrEmpty(extension.Extension))
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            try
            {
                await _ommClient.GetPPUserByNumberAsync(extension.Extension, cancellationToken);
                return new StatusCodeResult((int)HttpStatusCode.Gone);
            }
            catch(OmmNoEntryException) 
            {
                //expected
            }
            PPUserType ommUser;
            try
            {
                ommUser = await _ommClient.GetPPUserByNumberAsync(model.Caller, cancellationToken);
            }
            catch (OmmNoEntryException)
            {
                //caller doesn't exist in omm
                return new BadRequestResult();
            }

            var pp = await _ommClient.GetPPDevAsync(ommUser.Ppn, cancellationToken);
            var message = new GuruData
            {
                Ipei = pp.Ipei,
                Uak = pp.Uak,
                Extension = extension.Extension
            };
            _mgrContext.MessageQueue.Add(new MgrMessage
            {
                Type = GuruMessageType.AssignHandset,
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Json = message.Serialize()
            });

            cancellationToken = CancellationToken.None;//cancellation is no longer possible - we need to run till the end
            await _dectService.CreateExtension(new MgrCreateExtension
            {
                Number = extension.Extension,
                Name = extension.Name,
                Password = extension.Password,
                DectDevice = new GuruDect
                {
                    Ipei = pp.Ipei,
                    Uak = pp.Uak
                },
                Type = MgrExtensionType.DECT,
                Encryption = extension.UseEncryption,
                
            }, cancellationToken);

            await _mgrContext.SaveChangesAsync(cancellationToken);
            BackgroundJob.Enqueue<YateService>(x=>x.PlayWave($"AN016_{extension.Language}", extension.Extension, "9955", "DECT BASE", CancellationToken.None));
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
                .Where(x => x.Type == MgrExtensionType.DECT)
                .Select(x => x.Extension)
                .FirstOrDefaultAsync(cancellationToken);
            if (String.IsNullOrEmpty(tokenextension))
                return new StatusCodeResult((int)HttpStatusCode.NotFound);

            try
            {
                await _ommClient.GetPPUserByNumberAsync(tokenextension, cancellationToken);
                return new StatusCodeResult((int)HttpStatusCode.Gone);
            }
            catch (OmmNoEntryException)
            {
                //expected
            }

            return new StatusCodeResult((int)HttpStatusCode.OK);
        }
    }
}