using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Omm;
using mitelapi.Types;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Omm
{
    public class RFPsModel : PageModel
    {
        private readonly IOmmClient _ommClient;

        public List<RFPType> RFPs { get; set; }

        public List<TableHeader> TableHeaders { get; set;}

        public RFPsModel(IOmmClient client)
        {
            _ommClient = client;
        }

        public async Task OnGetAsync(RfpSortBy sortBy, OrderDirection sortOrder, CancellationToken cancellationToken)
        {
            //set defaults
            if (sortBy.Equals(RfpSortBy.NoSort)) {
                sortBy = RfpSortBy.Name;
            }
            if (sortOrder.Equals(OrderDirection.Unset)) {
                sortOrder = OrderDirection.Ascending;
            }
            
            //get data
            RFPs = await _ommClient.GetRFPAllAsync(true, true, cancellationToken);
            if (sortOrder.Equals(OrderDirection.Ascending)) {
                RFPs = RFPs.OrderBy(SortBy(sortBy)).ToList();
            } else {
                RFPs = RFPs.OrderByDescending(SortBy(sortBy)).ToList();
            }
            
            //create header
            TableHeaders = new List<TableHeader>() {
                new TableHeader { Label = "Id", SortBy = RfpSortBy.Id, SortOrder = OrderDirection.Unset },
                new TableHeader { Label = "RFP Name", SortBy = RfpSortBy.Name, SortOrder = OrderDirection.Unset },
                new TableHeader { Label = "MAC-Address", SortBy = RfpSortBy.MacAddress, SortOrder = OrderDirection.Unset },
                new TableHeader { Label = "IP-Address", SortBy = RfpSortBy.IpAddress, SortOrder = OrderDirection.Unset },
                new TableHeader { Label = "DECT Status"},
                new TableHeader { Label = "DECT Cluster", SortBy = RfpSortBy.Cluster, SortOrder = OrderDirection.Unset },
                new TableHeader { Label = "Reflective Env", SortBy = RfpSortBy.ReflectiveEnv, SortOrder = OrderDirection.Unset },
                new TableHeader { Label = "RFP Type", SortBy = RfpSortBy.Type, SortOrder = OrderDirection.Unset },
            };
            foreach(var header in TableHeaders) {
                if (header.SortBy.Equals(sortBy)) {
                    header.SortOrder = sortOrder;
                }
            }
        } 

        private Func<RFPType, object> SortBy(RfpSortBy sort) {
            switch (sort) {
                case RfpSortBy.Name:
                    return x => x.Name;
                case RfpSortBy.MacAddress:
                    return x => x.EthAddr;
                case RfpSortBy.IpAddress:
                    return x => x.IpAddr;
                case RfpSortBy.Cluster:
                    return x => x.Cluster;
                case RfpSortBy.ReflectiveEnv:
                    return x => x.ReflectiveEnv;
                case RfpSortBy.Type:
                    return x => x.HwType;
                default:
                    return x => x.Id;
            }
        }
    }



    public class TableHeader {
        public string Label { get; set; }

        public RfpSortBy SortBy { get; set; }

        public Enum SortOrder { get; set; }

        public string QuerysString() {
            var sortOrder = this.SortOrder.Equals(OrderDirection.Ascending) ? OrderDirection.Descending : OrderDirection.Ascending;
            return $"sortBy={SortBy}&sortOrder={sortOrder}";            
        }
    }

    public enum RfpSortBy {
        NoSort = 0,
        Id = 1,
        Name = 2,
        MacAddress = 3,
        IpAddress = 4,
        Cluster = 5,
        ReflectiveEnv = 6,
        Type = 7
    }

    public enum OrderDirection {
        Unset = 0,
        Ascending = 1,
        Descending = 2
    }
}