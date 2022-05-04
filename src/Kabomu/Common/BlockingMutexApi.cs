using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class BlockingMutexApi : IMutexApi
    {
        private readonly object _monitor;

        public BlockingMutexApi(object monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public void RunCallback(Action<object> cb, object cbState)
        {
            lock (_monitor)
            {
                cb.Invoke(cbState);
            }
        }
    }
}
