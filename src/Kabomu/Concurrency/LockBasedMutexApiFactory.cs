using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    public class LockBasedMutexApiFactory : IMutexApiFactory
    {
        public Task<IMutexApi> Create()
        {
            return Task.FromResult<IMutexApi>(new LockBasedMutexApi());
        }
    }
}
