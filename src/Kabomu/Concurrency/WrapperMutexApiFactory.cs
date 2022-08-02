using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Always returns the same instance of the <see cref="IMutexApi"/> class.
    /// </summary>
    public class WrapperMutexApiFactory : IMutexApiFactory
    {
        private readonly IMutexApi _mutexApi;

        /// <summary>
        /// Creates a new instance of the <see cref="WrapperMutexApiFactory"/> class.
        /// </summary>
        /// <param name="mutexApi">an instance of <see cref="IMutexApi"/> which will be used always
        /// be returned by <see cref="Create"/> method.</param>
        public WrapperMutexApiFactory(IMutexApi mutexApi)
        {
            if (mutexApi == null)
            {
                throw new ArgumentNullException(nameof(mutexApi));
            }
            _mutexApi = mutexApi;
        }

        /// <summary>
        /// Returns same instance of mutual exclusion api supplied at construction time.
        /// </summary>
        public Task<IMutexApi> Create()
        {
            return Task.FromResult(_mutexApi);
        }
    }
}
