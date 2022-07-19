using System;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Interface that generalizes mutual exclusion mechanisms.
    /// </summary>
    /// <remarks>
    /// This interface is central to writing thread-safe code in this library without assuming the use of locks.
    /// </remarks>
    public interface IMutexApi
    {
        /// <summary>
        /// Executes a callback synchronously or asynchronously under the guarantee that
        /// no other execution takes place until execution of provided callback is completed.
        /// </summary>
        /// <param name="cb">callback code to run under implementation-specific mutual exclusion scheme</param>
        void RunExclusively(Action cb);
    }
}