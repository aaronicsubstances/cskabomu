using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Used to manufacture <see cref="IMutexApi"/> instances for clients.
    /// </summary>
    public interface IMutexApiFactory
    {
        /// <summary>
        /// Returns an instance of the <see cref="IMutexApi"/> type. 
        /// </summary>
        /// <remarks>
        /// Depending on the implementation the instance may be a
        /// newly created instance or an existing instance.
        /// </remarks>
        /// <returns>a task whose result will be an instance of the <see cref="IMutexApi"/> type</returns>
        Task<IMutexApi> Create();
    }
}
