using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    public interface IEventLoopApi
    {
        bool IsInterimEventLoopThread { get; }

        /// <summary>
        /// Equivalent to setImmediate() and clearImmediate() in NodeJS
        /// </summary>
        /// <param name="cancellationToken">handle which can be used to clear immediate execution request</param>
        /// <param name="cb">callback to run</param>
        Task SetImmediate(CancellationToken cancellationToken, Func<Task> cb);

        /// <summary>
        /// Equivalent to combined setTimeout() and clearTimeout() in NodeJS
        /// </summary>
        /// <param name="millis">timeout value in milliseconds</param>
        /// <param name="cancellationToken">handle which can be used to clear timeout request</param>
        /// <param name="cb">callback to run on timeout.</param>
        Task SetTimeout(int millis, CancellationToken cancellationToken, Func<Task> cb);
    }
}
