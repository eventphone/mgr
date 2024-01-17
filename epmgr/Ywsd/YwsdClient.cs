using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace epmgr.Ywsd
{
    public class YwsdClient
    {
        public async Task<YwsdRouting> GetRoutingTreeAsync(string endpoint, string caller, string called, CancellationToken cancellationToken)
        {
            using (var client = GetClient(endpoint))
            {
                var builder = new QueryBuilder {{"caller", caller}, {"called", called}};
                var response = await client.GetAsync(builder.ToString(), cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<YwsdRouting>(cancellationToken);
            }
        }

        protected virtual HttpClient GetClient(string endpoint)
        {
            return new HttpClient
            {
                BaseAddress = new Uri(endpoint)
            };
        }
    }

    public class YwsdRouting
    {
        [JsonProperty("routing_tree")]
        public JObject Tree { get; set; }
        [JsonProperty("main_routing_result")]
        public JObject Result { get; set; }
        [JsonProperty("all_routing_results")]
        public JObject CacheEntries { get; set; }
        [JsonProperty("routing_status")]
        public string Status { get; set; }
        [JsonProperty("routing_status_details")]
        public string Details { get; set; }
    }
}
