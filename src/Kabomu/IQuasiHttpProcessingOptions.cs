using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu
{
    public interface IQuasiHttpProcessingOptions
    {
        IDictionary<string, object> Environment { get; }
        IMutexApi ProcessingMutexApi { get; }
    }
}
