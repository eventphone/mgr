using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Gsm;
using Xunit;

namespace epmgr.test
{
    public class GsmClientTest:IDisposable
    {
        private GsmClient _client;

        public GsmClientTest()
        {
            _client = new GsmClient("http://localhost.fiddler:8889/");
        }
        
        [Fact(Skip = "Integration")]
        public async Task CanPing()
        {
            await _client.PingAsync(CancellationToken.None);
        }

        [Fact(Skip="Integration")]
        public async Task CanGetSubscriber()
        {
            var info = await _client.GetSubscriberAsync("40010", CancellationToken.None);
            Assert.NotNull(info);
            Assert.Equal("1", info.ID);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}