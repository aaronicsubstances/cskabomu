using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class NullMutexApi : IMutexApi
    {
        public void PostCallback(Action<object> cb, object cbState)
        {
            cb.Invoke(cbState);
        }
    }
}
