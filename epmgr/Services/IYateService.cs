using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;

namespace epmgr.Services
{
    public interface IYateService
    {
        Task<IEnumerable<YateUser>> QueryAsync(string query, CancellationToken cancellationToken);

        Task SetTrunking(string extension, bool isTrunk, CancellationToken cancellationToken);
    }
}