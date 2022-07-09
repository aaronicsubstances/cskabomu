using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Concurrency
{
    public interface ISynchronizedEventLoopApi : IEventLoopApi, IMutexApi
    {
        bool IsInterimEventLoopThread { get; }
    }
}
