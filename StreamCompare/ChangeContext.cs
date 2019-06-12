using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NeoSmart.StreamCompare
{
    public readonly struct ChangeContext : IDisposable
    {
        private readonly SynchronizationContext _previous;

        public ChangeContext(SynchronizationContext newContext = null)
        {
            _previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(newContext);
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_previous);
        }
    }
}
