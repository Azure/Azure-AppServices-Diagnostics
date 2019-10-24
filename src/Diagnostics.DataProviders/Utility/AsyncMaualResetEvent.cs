using System;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders.Utility
{
    // Quote from https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-1-asyncmanualresetevent/
    public sealed class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();
        public Task WaitAsync() { return m_tcs.Task; }
        public void Set() { m_tcs.TrySetResult(true); }

        public void Reset()
        {
            while (true)
            {
                var tcs = m_tcs;
                if (!tcs.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref m_tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                    return;
            }
        }
    }
}
