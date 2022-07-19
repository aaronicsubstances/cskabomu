using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides helpful functions for concurrency control in the Kabomu library.
    /// </summary>
    public static class ConcurrencyExtensions
    {
        /// <summary>
        /// Enables the use of synchronous and asynchronous mutual exclusion schemes with the syntax:
        /// <code>
        /// using (await mutexApi.Synchronize())
        /// {
        ///     /* put protected code here */
        /// }
        /// </code>
        /// </summary>
        /// <param name="mutexApi">the mutual exclusion scheme. can be null.</param>
        /// <returns>an object which implements the awaiter protocol needed for mutual exclusion scheme to work
        /// like "lock" keyword, but through the use of "using" and "async/await" keywords.</returns>
        public static MutexAwaitable Synchronize(this IMutexApi mutexApi)
        {
            return new MutexAwaitable(mutexApi);
        }
    }
}
