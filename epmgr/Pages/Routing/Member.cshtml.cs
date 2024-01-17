using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data.ywsd;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace epmgr.Pages.Routing
{
    public class MemberModel : PageModel
    {
        private readonly YwsdDbContext _context;

        [BindProperty(SupportsGet = true)]
        public int ForkRankId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ExtensionId { get; set; }

        [BindProperty]
        public ForkRankMember Member { get; set; }

        public Extension Extension { get; private set; }

        public string Error { get; private set; }

        public MemberModel(YwsdDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            if (ExtensionId is null)
            {
                await LoadCreateAsync(cancellationToken);
                return Page();
            }
            Member = await _context.ForkRankMember
                .AsNoTracking()
                .Where(x => x.ExtensionId == ExtensionId.Value)
                .Where(x => x.ForkRankId == ForkRankId)
                .Select(x => new ForkRankMember
                {
                    Extension = new Extension{Number = x.Extension.Number},
                    ForkRank = new Data.ywsd.ForkRank{Index = x.ForkRank.Index},
                    IsActive = x.IsActive,
                    Type = x.Type
                })
                .FirstOrDefaultAsync(cancellationToken);
            Extension = await _context.ForkRanks
                .AsNoTracking()
                .Where(x => x.Id == ForkRankId)
                .Select(x => new Extension
                {
                    Id = x.ExtensionId,
                    Number = x.Extension.Number
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (Member is null || Extension is null) return NotFound();
            return Page();
        }

        private async Task LoadCreateAsync(CancellationToken cancellationToken)
        {

            if (ExtensionId is null)
            {
                var parents = await _context.ForkRanks
                    .Where(x => x.Id == ForkRankId)
                    .Select(x => new
                    {
                        x.Extension.Number,
                        x.ExtensionId,
                        x.Index
                    })
                    .FirstAsync(cancellationToken);
                Member = new ForkRankMember
                {
                    ForkRank = new Data.ywsd.ForkRank {Index = parents.Index}
                };
                Extension = new Extension {Id = parents.ExtensionId, Number = parents.Number};
            }
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (Member is null) return RedirectToPage();
            ForkRankMember member;
            if (ExtensionId is null)
            {
                member = new ForkRankMember
                {
                    ForkRankId = ForkRankId
                };
                _context.ForkRankMember.Add(member);
            }
            else
            {
                member = await _context.ForkRankMember
                    .Where(x => x.ExtensionId == ExtensionId.Value)
                    .Where(x => x.ForkRankId == ForkRankId)
                    .FirstAsync(cancellationToken);
            }
            member.IsActive = Member.IsActive;
            member.Type = Member.Type;
            try
            {
                var id = await _context.Extensions
                    .Where(x => x.Number == Member.Extension.Number)
                    .Select(x => new {x.Id})
                    .FirstOrDefaultAsync(cancellationToken);
                if (id is null)
                {
                    ModelState.AddModelError($"{nameof(Member)}.{nameof(Member.Extension)}.{nameof(Member.Extension.Number)}", "invalid extension");
                    var parents = await _context.ForkRanks
                        .Where(x => x.Id == ForkRankId)
                        .Select(x => new
                        {
                            x.Extension.Number,
                            x.ExtensionId,
                            x.Index
                        })
                        .FirstAsync(cancellationToken);
                    Member = new ForkRankMember
                    {
                        ForkRank = new Data.ywsd.ForkRank {Index = parents.Index}
                    };
                    Extension = new Extension {Id = parents.ExtensionId, Number = parents.Number};
                    return Page();
                }
                member.ExtensionId = id.Id;
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (NpgsqlException ex)
            {
                Error = ex.Message;
                await LoadCreateAsync(cancellationToken);
                return Page();
            }
            catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException)
            {
                Error = ex.InnerException.Message;
                await LoadCreateAsync(cancellationToken);
                return Page();
            }
            return RedirectToPage("ForkRank", new {Id = ForkRankId});
        }

        public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
        {
            if (ExtensionId is null) return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var member = await _context.ForkRankMember
                .Where(x => x.ForkRankId == ForkRankId)
                .Where(x => x.ExtensionId == ExtensionId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (member is not null) 
            {
                _context.ForkRankMember.Remove(member);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return RedirectToPage("ForkRank", new {Id = ForkRankId});
        }
    }
}