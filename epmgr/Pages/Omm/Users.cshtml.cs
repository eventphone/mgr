using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm
{
    public class UsersModel : PageModel
    {
        private readonly IOmmClient _ommClient;

        [BindProperty(SupportsGet = true, Name = "q")]
        public string Query { get; set; }

        public List<PPUserType> Users { get; } = new List<PPUserType>();

        public UsersModel(IOmmClient client)
        {
            _ommClient = client;
        }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            var all = await _ommClient.GetPPAllUserAsync(cancellationToken);
            if (!String.IsNullOrEmpty(Query))
            {
                foreach (var user in all)
                {
                    if (Contains(user.Uid, Query))
                    {
                        Users.Add(user);
                    }
                    else if (Contains(user.Ppn, Query))
                    {
                        Users.Add(user);
                    }
                    else if (Contains(user.Num, Query))
                    {
                        Users.Add(user);
                    }
                    else if (Contains(user.Name, Query))
                    {
                        Users.Add(user);
                    }
                    else if (Contains(user.Hierarchy1, Query))
                    {
                        Users.Add(user);
                    }
                }
            }
            else
            {
                Users.AddRange(all.Where(x => x.Ppn == 0));
            }
        }

        private static bool Contains(object item, string value)
        {
            if (item is null) return false;
            return item.ToString().ToLowerInvariant().Contains(value.ToLowerInvariant());
        }
    }
}