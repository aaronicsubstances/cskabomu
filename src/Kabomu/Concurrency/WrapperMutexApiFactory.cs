using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Always returns the same instance of <see cref="IMutexApi"/> class provided at construction time.
    /// </summary>
    public class WrapperMutexApiFactory : IMutexApiFactory
    {
        private readonly IMutexApi _mutexApi;

        /// <summary>
        /// Creates a new instance with a provied mutual exclusion api.
        /// </summary>
        /// <param name="mutexApi">mutual exclusion api.</param>
        public WrapperMutexApiFactory(IMutexApi mutexApi)
        {
            if (mutexApi == null)
            {
                throw new ArgumentNullException(nameof(mutexApi));
            }
            _mutexApi = mutexApi;
        }

        /// <summary>
        /// Returns same instance of mutual exclusion api.
        /// </summary>
        /// <returns>mutual exclusion api provided at time of construction</returns>
        public Task<IMutexApi> Create()
        {
            return Task.FromResult(_mutexApi);
        }
    }
}
