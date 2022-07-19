using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Interface which tries to unite the best possible conditions for using the expected implementations of
    /// IMutexApi - locks and event loops - for use by async/await and using keywords in C#. The 
    /// <see cref="MutexAwaitable.MutexAwaiter"/> struct reveals how it is used.
    /// </summary>
    public interface IMutexContextFactory
    {
        /// <summary>
        /// Used to continue synchronous execution for implementations of IMutexApi which
        /// block threads to ensure mutual exclusion (ie locks); as well as implementations which have
        /// no further step to take to ensure mutual exclusion (ie when inside an event loop thread).
        /// </summary>
        bool IsExclusiveRunRequired { get; }

        /// <summary>
        /// Used by IMutexApi implementations which need to take actions before and after running callbacks in order
        /// to enforce mutual exclusion correctly (ie locks). Implementations which do not need this may return null
        /// </summary>
        /// <remarks>
        /// IDisposable was chosen just for the C# syntatic convenience of using it like how "lock" keyword are used,
        /// but with "using" keyword.
        /// </remarks>
        /// <returns>null or a IDisposable instance whose construction starts an implementation-defined mutal exclusion scheme,
        /// and whose Dispose() method ends the mutual exclusion scheme</returns>
        IDisposable CreateMutexContext();
    }
}
