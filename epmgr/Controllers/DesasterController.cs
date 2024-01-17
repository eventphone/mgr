using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace epmgr.Controllers
{
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class DesasterController : Controller
    {
        private readonly MgrDbContext _context;
        private readonly Settings _settings;

        public DesasterController(MgrDbContext context, IOptions<Settings> options)
        {
            _context = context;
            _settings = options.Value;
        }

        [HttpPost]
        public async Task<StatusCodeResult> Start([FromForm] string pin, CancellationToken cancellationToken)
        {
            if (!Request.Headers.ContainsKey("apikey"))
                return new UnauthorizedResult();
            if (Request.Headers["apikey"].All(x => x != _settings.ApiKey))
                return new UnauthorizedResult();
            if (String.IsNullOrEmpty(pin)) return BadRequest();

            var desaster = await _context.DesasterCalls
                .Where(x => x.Pin == pin)
                .FirstOrDefaultAsync(cancellationToken);
            if (desaster is null) return NotFound();
            BackgroundJob.Enqueue<DesasterCaller>(x => x.GenerateCallsAsync(desaster.Name, desaster.Announcement, desaster.Target, CancellationToken.None));
            return Ok();
        }
    }
}