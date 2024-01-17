using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Gsm;
using epmgr.Model;

namespace epmgr.Pages.Gsm
{
    public class IndexModel : ListModelBase
    {
        private readonly IGsmClient _gsmClient;
        public SubscriberInfo[] Subscribers { get; set; }

        public IndexModel(IGsmClient gsmClient)
        {
            _gsmClient = gsmClient;
        }

        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            IEnumerable<SubscriberInfo> subscribers = await _gsmClient.GetSubscribersAsync(cancellationToken);
            Total = subscribers.LongCount();
            if (!String.IsNullOrEmpty(Search))
            {
                var keywords = Search.Split();
                foreach (var keyword in keywords)
                {
                    subscribers = subscribers.Where(x => x.Number.Contains(keyword) || x.IMSI.Contains(keyword));
                }
            }
            Subscribers = subscribers
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToArray();
        }
    }
}