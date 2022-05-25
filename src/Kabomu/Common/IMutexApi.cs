using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IMutexApi
    {
        void RunExclusively(Action<object> cb, object cbState);
    }
}
