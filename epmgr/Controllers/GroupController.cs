using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Data.ywsd;
using epmgr.Guru;
using epmgr.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace epmgr.Controllers
{
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class GroupController : Controller
    {
        private readonly YwsdDbContext _context;
        private readonly MgrDbContext _mgrContext;
        private readonly Settings _settings;

        public GroupController(YwsdDbContext context, MgrDbContext mgrContext, IOptions<Settings> options)
        {
            _context = context;
            _mgrContext = mgrContext;
            _settings = options.Value;
        }

        [HttpPost]
        public Task<IActionResult> ActivateAsync([FromBody] GroupStateModel model, CancellationToken cancellationToken)
        {
            return SetActiveStateAsync(model.Group, model.Member, true, cancellationToken);
        }

        [HttpPost]
        public Task<IActionResult> PauseAsync([FromBody] GroupStateModel model, CancellationToken cancellationToken)
        {
            return SetActiveStateAsync(model.Group, model.Member, false, cancellationToken);
        }

        private async Task<IActionResult> SetActiveStateAsync(string group, string member, bool isActive, CancellationToken cancellationToken)
        {
            if (!Request.Headers.ContainsKey("apikey"))
                return Unauthorized();
            if (Request.Headers["apikey"].All(x => x != _settings.ApiKey))
                return Unauthorized();
            if (!ModelState.IsValid)
                return BadRequest();

            var groupMember = await _context.Extensions
                .Where(x => x.Number == group)
                .SelectMany(x => x.ForkRanks)
                .SelectMany(x => x.ForkRankMember)
                .Where(x => x.Extension.Number == member)
                .FirstOrDefaultAsync(cancellationToken);

            if (groupMember is null) return NotFound();
            groupMember.IsActive = isActive;
            await _context.SaveChangesAsync(cancellationToken);

            var message = new GuruData
            {
                Extension = group,
                Extensions = new[] {new GuruExtension {Extension = member, Active = isActive},}
            };
            _mgrContext.MessageQueue.Add(new MgrMessage
            {
                Type = GuruMessageType.UpdateMember,
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Json = message.Serialize()
            });
            await _mgrContext.SaveChangesAsync(cancellationToken);

            return Ok();
        }
    }
}