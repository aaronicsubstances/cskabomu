using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    public interface IMutexApiFactory
    {
        Task<IMutexApi> Create();
    }
}
