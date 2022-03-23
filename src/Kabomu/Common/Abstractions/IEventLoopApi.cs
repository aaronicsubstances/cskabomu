using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IEventLoopApi
    {
        long CurrentTimestamp { get; }
        void PostCallback(Action<object> cb, object cbState);
        object ScheduleTimeout(int millis, Action<object> cb, object cbState);
        void CancelTimeout(object id);
        ErrorHandler ErrorHandler { get; set; }
    }
}
