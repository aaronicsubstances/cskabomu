using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Common interface of classes in Kabomu library which perform
    /// resource clean-up operations.
    /// </summary>
    /// <remarks>
    /// This interface exists as a simpler alternative to the Dispose and
    /// DisposeAsync resource clean-up protocols. 
    /// </remarks>
    public interface ICustomDisposable
    {
        /// <summary>
        /// Gets a function which if invoked,
        /// performs any needed clean up operation on resources held
        /// by the instance.
        /// </summary>
        Func<Task> Disposer { get; }
    }
}
