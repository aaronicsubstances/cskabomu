using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    public static class ConcurrencyExtensions
    {
        public static MutexAwaitable Synchronize(this IMutexApi mutexApi)
        {
            return new MutexAwaitable(mutexApi);
        }
    }
}
