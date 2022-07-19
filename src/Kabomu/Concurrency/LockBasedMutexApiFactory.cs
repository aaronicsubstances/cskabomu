using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Manufactures instances of <see cref="LockBasedMutexApi"/> class.
    /// </summary>
    public class LockBasedMutexApiFactory : IMutexApiFactory
    {
        /// <summary>
        /// Creates and returns a new instance of <see cref="LockBasedMutexApi"/> class.
        /// </summary>
        /// <returns>a new instance of <see cref="LockBasedMutexApi"/> class with an internally generated lock
        /// suitable for mutual exclusion</returns>
        public Task<IMutexApi> Create()
        {
            return Task.FromResult<IMutexApi>(new LockBasedMutexApi());
        }
    }
}
