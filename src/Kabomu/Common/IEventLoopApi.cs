using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IEventLoopApi : IMutexApi
    {
        long CurrentTimestamp { get; }

        /// <summary>
        /// Equivalent to non-cancellalable setImmediate() in NodeJS
        /// </summary>
        /// <param name="cb"></param>
        /// <param name="cbState"></param>
        void PostCallback(Action<object> cb, object cbState);

        /// <summary>
        /// Equivalent to setTimeout() in NodeJS
        /// </summary>
        /// <param name="millis"></param>
        /// <param name="cb"></param>
        /// <param name="cbState"></param>
        /// <returns>a handle with which timeout can be cancelled</returns>
        object ScheduleTimeout(int millis, Action<object> cb, object cbState);

        /// <summary>
        /// Equivalent to clearTimeout() in NodeJS
        /// </summary>
        /// <param name="id"></param>
        void CancelTimeout(object id);

        /// <summary>
        /// Used to report callback execution errors.
        /// </summary>
        UncaughtErrorCallback ErrorHandler { get; set; }
    }
}
