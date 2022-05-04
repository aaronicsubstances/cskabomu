using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class NullMutexApi : IMutexApi
    {
        public void RunCallback(Action<object> cb, object cbState)
        {
            cb.Invoke(cbState);
        }
    }
}
