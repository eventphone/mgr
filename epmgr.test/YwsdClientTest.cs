using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Ywsd;
using Moq;
using Moq.Protected;
using Xunit;

namespace epmgr.test
{
    public class YwsdClientTest
    {
        private readonly Mock<HttpMessageHandler> _httpHandler = new Mock<HttpMessageHandler>();

        private readonly string _response ="{\"routing_tree\": {\"id\": 1, \"yate_id\": null, \"extension\": \"2000\", \"name\": \"PoC\", \"outgoing_extension\": null," +
                                           " \"outgoing_name\": null, \"ringback\": null, \"forwarding_delay\": null, \"forwarding_extension_id\": null, \"lang\": \"de_DE\"," +
                                           " \"type\": \"Type.GROUP\", \"forwarding_mode\": \"ForwardingMode.DISABLED\", \"tree_identifier\": \"1\", \"logs\": []," +
                                           " \"fork_ranks\": [{\"id\": 1, \"extension_id\": 1, \"index\": 0, \"delay\": null, \"mode\": \"Mode.DEFAULT\"," +
                                           " \"tree_identifier\": \"1-fr1\", \"logs\": [{\"msg\": \"Discovery aborted for <Extension 2002, name=PoC Bernie," +
                                           " type=Type.SIMPLE> in <ForkRank id=1, extension_id=1, index=Mode.DEFAULT, mode=0, delay=Mode.DEFAULT>, was already present.\\nTemporarily disable membership for this routing.\"," +
                                           " \"level\": \"WARN\", \"related_node\": \"1-fr1-3\"}], \"members\": [{\"type\": \"RankMemberType.DEFAULT\", \"active\": true," +
                                           " \"extension\": {\"id\": 2, \"yate_id\": 2, \"extension\": \"2001\", \"name\": \"PoC Sascha\", \"outgoing_extension\": null," +
                                           " \"outgoing_name\": null, \"ringback\": null, \"forwarding_delay\": null, \"forwarding_extension_id\": null, \"lang\": \"de_DE\"," +
                                           " \"type\": \"Type.MULTIRING\", \"forwarding_mode\": \"ForwardingMode.DISABLED\", \"tree_identifier\": \"1-fr1-2\", \"logs\": []," +
                                           " \"fork_ranks\": [{\"id\": 2, \"extension_id\": 2, \"index\": 0, \"delay\": null, \"mode\": \"Mode.DEFAULT\"," +
                                           " \"tree_identifier\": \"1-fr1-2-fr2\", \"logs\": [], \"members\": [{\"type\": \"RankMemberType.DEFAULT\", \"active\": true," +
                                           " \"extension\": {\"id\": 5, \"yate_id\": 2, \"extension\": \"2005\", \"name\": \"PoC Sascha (SIP)\", \"outgoing_extension\": null," +
                                           " \"outgoing_name\": null, \"ringback\": null, \"forwarding_delay\": null, \"forwarding_extension_id\": null, \"lang\": \"de_DE\"," +
                                           " \"type\": \"Type.SIMPLE\", \"forwarding_mode\": \"ForwardingMode.DISABLED\", \"tree_identifier\": \"1-fr1-2-fr2-5\", \"logs\": []}}]}]}}," +
                                           " {\"type\": \"RankMemberType.DEFAULT\", \"active\": false, \"extension\": {\"id\": 3, \"yate_id\": 2, \"extension\": \"2002\"," +
                                           " \"name\": \"PoC Bernie\", \"outgoing_extension\": null, \"outgoing_name\": null, \"ringback\": null, \"forwarding_delay\": null," +
                                           " \"forwarding_extension_id\": null, \"lang\": \"de_DE\", \"type\": \"Type.SIMPLE\", \"forwarding_mode\": \"ForwardingMode.DISABLED\"," +
                                           " \"tree_identifier\": \"1-fr1-3\", \"logs\": []}}, {\"type\": \"RankMemberType.DEFAULT\", \"active\": true, \"extension\": {\"id\": 4," +
                                           " \"yate_id\": 2, \"extension\": \"2004\", \"name\": \"PoC BeF\", \"outgoing_extension\": null, \"outgoing_name\": null, \"ringback\": null," +
                                           " \"forwarding_delay\": null, \"forwarding_extension_id\": null, \"lang\": \"de_DE\", \"type\": \"Type.SIMPLE\"," +
                                           " \"forwarding_mode\": \"ForwardingMode.DISABLED\", \"tree_identifier\": \"1-fr1-4\", \"logs\": []}}, {\"type\": \"RankMemberType.DEFAULT\"," +
                                           " \"active\": true, \"extension\": {\"id\": 6, \"yate_id\": 2, \"extension\": \"2042\", \"name\": \"PoC Garwin\", \"outgoing_extension\": null," +
                                           " \"outgoing_name\": null, \"ringback\": null, \"forwarding_delay\": null, \"forwarding_extension_id\": null, \"lang\": \"de_DE\"," +
                                           " \"type\": \"Type.SIMPLE\", \"forwarding_mode\": \"ForwardingMode.DISABLED\", \"tree_identifier\": \"1-fr1-6\", \"logs\": []}}]}]}," +
                                           " \"routing_result\": {\"target\": {\"target\": \"lateroute/stage1-547744baa7404caea4868c4f9bfeab6f-1\"," +
                                           " \"parameters\": {\"x_eventphone_id\": \"547744baa7404caea4868c4f9bfeab6f\", \"osip_X-Eventphone-Id\": \"547744baa7404caea4868c4f9bfeab6f\"," +
                                           " \"callername\": \"PoC Bernie\", \"osip_X-Caller-Language\": \"de_DE\"}}, \"fork_targets\": [{\"target\": \"lateroute/stage1-547744baa7404caea4868c4f9bfeab6f-1-2\"," +
                                           " \"parameters\": {\"x_eventphone_id\": \"547744baa7404caea4868c4f9bfeab6f\", \"osip_X-Eventphone-Id\": \"547744baa7404caea4868c4f9bfeab6f\"," +
                                           " \"callername\": \"PoC Bernie\", \"osip_X-Caller-Language\": \"de_DE\"}}, {\"target\": \"lateroute/2004\"," +
                                           " \"parameters\": {\"eventphone_stage2\": \"1\", \"x_eventphone_id\": \"547744baa7404caea4868c4f9bfeab6f\", \"osip_X-Eventphone-Id\": \"547744baa7404caea4868c4f9bfeab6f\"}}," +
                                           " {\"target\": \"lateroute/2042\", \"parameters\": {\"eventphone_stage2\": \"1\", \"x_eventphone_id\": \"547744baa7404caea4868c4f9bfeab6f\"," +
                                           " \"osip_X-Eventphone-Id\": \"547744baa7404caea4868c4f9bfeab6f\"}}]}, \"routing_cache_entries\": " +
                                           "{\"lateroute/stage1-547744baa7404caea4868c4f9bfeab6f-1-2\": {\"target\": {\"target\": \"lateroute/stage1-547744baa7404caea4868c4f9bfeab6f-1-2\"," +
                                           " \"parameters\": {\"x_eventphone_id\": \"547744baa7404caea4868c4f9bfeab6f\", \"osip_X-Eventphone-Id\": \"547744baa7404caea4868c4f9bfeab6f\"," +
                                           " \"callername\": \"PoC Bernie\", \"osip_X-Caller-Language\": \"de_DE\"}}, \"fork_targets\": [{\"target\": \"lateroute/2001\"," +
                                           " \"parameters\": {\"eventphone_stage2\": \"1\", \"x_eventphone_id\": \"547744baa7404caea4868c4f9bfeab6f\", \"osip_X-Eventphone-Id\": \"547744baa7404caea4868c4f9bfeab6f\"}}," +
                                           " {\"target\": \"lateroute/2005\", \"parameters\": {\"eventphone_stage2\": \"1\", \"x_eventphone_id\": \"547744baa7404caea4868c4f9bfeab6f\"," +
                                           " \"osip_X-Eventphone-Id\": \"547744baa7404caea4868c4f9bfeab6f\"}}]}}, \"routing_status\": \"PROCESSING\", \"routing_status_details\": \"\"}";

        [Fact]
        public async Task CanGetRoutingTree()
        {
            var client = new TestClient(_httpHandler.Object);
            _httpHandler.Protected().Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.Query.Contains("caller=2002") && x.RequestUri.Query.Contains("called=2000")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_response, Encoding.UTF8,"text/json")
                });
            var result = await client.GetRoutingTreeAsync(null, "2002", "2000", CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal("PROCESSING", result.Status);
        }

        class TestClient : YwsdClient
        {
            private readonly HttpMessageHandler _handler;
            private static readonly string _baseAddress= "http://localhost:1337";

            public TestClient(HttpMessageHandler handler)
            {
                _handler = handler;
            }

            protected override HttpClient GetClient(string endpoint)
            {
                return new HttpClient(_handler)
                {
                    BaseAddress = new Uri(_baseAddress)
                };
            }
        }
    }
}