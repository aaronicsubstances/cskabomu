using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    public class WrapperMutexApiFactory : IMutexApiFactory
    {
        private readonly IMutexApi _mutexApi;

        public WrapperMutexApiFactory(IMutexApi mutexApi)
        {
            if (mutexApi == null)
            {
                throw new ArgumentException("null mutex api");
            }
            _mutexApi = mutexApi;
        }

        public Task<IMutexApi> Create()
        {
            return Task.FromResult(_mutexApi);
        }
    }
}
