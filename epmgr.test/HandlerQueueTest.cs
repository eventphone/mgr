using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Guru;
using epmgr.Services;
using Xunit;
using Xunit.Abstractions;

namespace epmgr.test
{
    public class HandlerQueueTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ConcurrentStack<(long,int,string)> _result = new ConcurrentStack<(long,int,string)>();

        public HandlerQueueTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private GuruHandlerQueue GetQueue(Action<(long, int, string)> handler)
        {
            var mgr = new DummyHandler ( 10, false, "mgr",  1, handler);
            var ywsd = new DummyHandler( 20, false, "ywsd", 1, handler);
            var ysip = new DummyHandler( 30, false, "ysip", 1, handler);
            var yapp = new DummyHandler( 30, false, "yapp", 1, handler);
            var ygsm = new DummyHandler( 30, false, "ygsm", 1, handler);
            var ydect = new DummyHandler( 30, false, "ygsm", 5, handler);
            var ypremium = new DummyHandler( 30, false, "ypremium", 1, handler);
            var omm = new DummyHandler(40, true, "omm", 2, handler);
            var gsm = new DummyHandler(40, true, "gsm", 5, handler);
            var ack = new DummyHandler(Int32.MaxValue, true, "ack", 2, handler);

            return GuruHandlerQueue.Create(new[] {ack, gsm, ypremium, mgr, ywsd, ysip, omm, yapp, ygsm, ydect});
        }

        [Fact]
        public async Task MessagesAreProcessedInOrder()
        {
            Action<(long,int,string)> handler = x=>_result.Push(x);

            var queue = GetQueue(handler);

            var messages = Enumerable.Range(2000, 40).Select(x => new GuruMessage {Id = x});
            await Task.WhenAll(messages.Select(x => queue.ProcessMessageAsync(x, CancellationToken.None)));

            var previous = (0L,0,"");
            foreach (var item in _result.Reverse())
            {
                _testOutputHelper.WriteLine($"{item.Item1}|{item.Item2}|{item.Item3}");
                Assert.True(previous.Item1 < item.Item1 || previous.Item2 <= item.Item2);
                previous = item;
            }
        }

        [Fact]
        public async Task FailedHandlerStopsPipeline()
        {
            bool failed = false;
            bool processed = false;
            Action<(long, int, string)> handler = x =>
            {
                if (x.Item1 == 2002)
                {
                    if (x.Item2 == 20) throw new NotImplementedException("broken handler");
                    if (x.Item2 > 20) failed = true;
                }
                processed = true;
            };
            
            var queue = GetQueue(handler);

            var messages = Enumerable.Range(2000, 40).Select(x => new GuruMessage {Id = x});
            var task = Task.WhenAll(messages.Select(x => queue.ProcessMessageAsync(x, CancellationToken.None)));
            var ex = await Assert.ThrowsAsync<GuruSyncException>(() => task);
            Assert.Equal("broken handler", ex.Message);

            Assert.False(failed);
            Assert.True(processed);
        }

        class DummyHandler : IGuruMessageHandler
        {
            public DummyHandler(int order, bool isThreadSafe, string identity, int delay, Action<(long,int,string)> handler)
            {
                Order = order;
                IsThreadSafe = isThreadSafe;
                Identity = identity;
                Delay = delay;
                Handler = handler;
            }

            public int Order { get; }

            public bool IsThreadSafe { get; }

            public string Identity { get; }

            public int Delay { get; }

            public Action<(long,int,string)> Handler { get; }

            private bool _isProcessing;
            public async Task ProcessMessageAsync(GuruMessage guruMessage, CancellationToken cancellationToken)
            {
                if (_isProcessing && !IsThreadSafe) throw new InvalidOperationException();
                _isProcessing = true;
                await Task.Delay(Delay*10, cancellationToken);
                Handler((guruMessage.Id, Order, Identity));
                _isProcessing = false;
            }
        }
    }
}