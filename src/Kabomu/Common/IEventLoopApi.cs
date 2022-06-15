using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IEventLoopApi
    {
        /// <summary>
        /// Equivalent to setImmediate() and clearImmediate() in NodeJS
        /// </summary>
        /// <param name="cancellationToken">handle which can be used to clear immediate execution request</param>
        Task SetImmediateAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Equivalent to combined setTimeout() and clearTimeout() in NodeJS
        /// </summary>
        /// <param name="millis"></param>
        /// <param name="cancellationToken">handle which can be used to clear timeout request</param>
        Task SetTimeoutAsync(int millis, CancellationToken cancellationToken);

        /// <summary>
        /// Used to report task execution errors.
        /// </summary>
        UncaughtErrorCallback ErrorHandler { get; set; }

        bool IsMutexRequired(out Task taskToWrap);
        Task MutexWrap(Task t);
        Task<T> MutexWrap<T>(Task<T> taskToWrap);
    }
}
