using System;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Represents mutual exclusion mechanisma in an abstract way. Useful for writing thread-safe library code
    /// without assuming any particular mutual exclusion strategy such as locks.
    /// </summary>
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