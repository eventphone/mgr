using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Model
{
    public interface IYatePageModel
    {
        string UserId { get; }
    }

    public abstract class YatePageModel<TSyncService, TUser> : PageModel, IYatePageModel where TUser:YateUser, new() where TSyncService:YateSyncService<TUser>
    {
        private readonly TSyncService _service;
        protected readonly MgrDbContext _mgrContext;

        public abstract string UserId { get; }

        protected YatePageModel(TSyncService service, MgrDbContext mgrContext)
        {
            _service = service;
            _mgrContext = mgrContext;
        }

        protected Task<TUser> GetUserAsync(string username, CancellationToken cancellationToken)
        {
            return _service.GetUserAsync(username, cancellationToken);
        }

        protected IQueryable<TUser> GetUserQuery(string username)
        {
            return _service.GetUserQuery(username);
        }

        protected Task DeleteUserAsync(string username, CancellationToken cancellationToken)
        {
            return _service.DeleteExtension(username, cancellationToken);
        }
        
        protected virtual Task<string> GetSecretAsync(string id, CancellationToken cancellationToken)
        {
            return _mgrContext.Extensions.Where(x => x.Extension == id).Select(x=>x.Password).FirstOrDefaultAsync(cancellationToken);
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim == null)
                return -1;
            return Int32.Parse(idClaim.Value);
        }
    }
}