using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Guru;

namespace epmgr.Services
{
    public class GuruHandlerQueue
    {
        private readonly IList<Handler> _handler;

        private GuruHandlerQueue(IEnumerable<IGuruMessageHandler> handler)
        {
            _handler = handler.Select(x=>new Handler(x)).ToList();
        }

        private GuruHandlerQueue Next { get; set; }

        public async Task ProcessMessageAsync(GuruMessage message, CancellationToken cancellationToken)
        {
            await Task.WhenAll(_handler.Select(x => x.Queue(message, cancellationToken)));
            if (Next is null) return;
            await Next.ProcessMessageAsync(message, cancellationToken);
        }

        class Handler
        {
            private readonly IGuruMessageHandler _handler;

            private Task _current = Task.CompletedTask;

            private readonly object _syncRoot = new object();

            public Handler(IGuruMessageHandler handler)
            {
                _handler = handler;
            }

            public Task Queue(GuruMessage message, CancellationToken cancellationToken)
            {
                if (_handler.IsThreadSafe)
                    return Catch(_handler.ProcessMessageAsync(message, cancellationToken), message);

                lock (_syncRoot)
                {
                    return _current = Catch(ContinueWith(_current, message, cancellationToken), message);
                }
            }

            private async Task ContinueWith(Task parent, GuruMessage message, CancellationToken cancellationToken)
            {
                await parent;
                await _handler.ProcessMessageAsync(message, cancellationToken);
            }

            private async Task Catch(Task inner, GuruMessage message)
            {
                try
                {
                    await inner;
                }
                catch (Exception ex)
                {
                    throw new GuruSyncException(message, ex);
                }
            }
        }

        public static GuruHandlerQueue Create(IEnumerable<IGuruMessageHandler> handlers)
        {
            var queue = handlers
                .GroupBy(x => x.Order)
                .OrderBy(x=>x.Key)
                .Select(x => new GuruHandlerQueue(x))
                .ToList();
            if (queue.Count == 0) return null;
            for (int i = 0; i < queue.Count-1; i++)
            {
                queue[i].Next = queue[i + 1];
            }
            return queue[0];
        }
    }

    public class GuruSyncException:Exception
    {
        public GuruSyncException(GuruMessage message, Exception inner):base(inner.Message, inner)
        {
            GuruMessage = message;
        }

        public GuruMessage GuruMessage { get; }
    }
}