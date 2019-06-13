using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NeoSmart.StreamCompare
{
    internal static class ChangeContext
    {
        private readonly struct InnerChangeContext : IDisposable
        {
            private readonly SynchronizationContext _previous;

            public InnerChangeContext(SynchronizationContext newContext)
            {
                _previous = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(newContext);
            }

            public void Dispose()
            {
                SynchronizationContext.SetSynchronizationContext(_previous);
            }
        }

        public static IDisposable To(SynchronizationContext newContext)
        {
            return new InnerChangeContext(newContext);
        }

        public static IDisposable NoContext()
        {
            return To(null);
        }
    }
}
