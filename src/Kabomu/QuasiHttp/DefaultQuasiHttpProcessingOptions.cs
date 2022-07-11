using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpProcessingOptions : IQuasiHttpProcessingOptions
    {
        public IDictionary<string, object> Environment { get; set; }
        public IMutexApi ProcessingMutexApi { get; set; }
        public int MaxChunkSize { get; set; }
    }
}
