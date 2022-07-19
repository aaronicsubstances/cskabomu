using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Used to manufacture IMutexApi instances for clients.
    /// </summary>
    public interface IMutexApiFactory
    {
        /// <summary>
        /// Returns an instance of IMutexApi. Depending on implementation the instance may be a
        /// newly created instance.
        /// </summary>
        /// <returns>a task whose result is a IMutexApi instance.</returns>
        Task<IMutexApi> Create();
    }
}
