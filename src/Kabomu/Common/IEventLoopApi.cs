using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IEventLoopApi : IMutexApi
    {
        long CurrentTimestamp { get; }
        void PostCallback(Action<object> cb, object cbState);
        object ScheduleTimeout(int millis, Action<object> cb, object cbState);
        void CancelTimeout(object id);
        UncaughtErrorCallback ErrorHandler { get; set; }

        /*bool IsEventDispatchThread { get; }
        bool IsSuperiorPeriodicTimeoutAvailable { get; }
        object SchedulePeriodicTimeout(int millis, Action<object> cb, object cbState);
        void CancelPeriodicTimeout(object id);*/
    }
}
