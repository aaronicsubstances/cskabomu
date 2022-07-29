using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Server
{
    /// <summary>
    /// Implementation of <see cref="IQuasiHttpProcessingOptions"/> providing mutable versions of
    /// all properties in interface.
    /// </summary>
    public class DefaultQuasiHttpProcessingOptions : IQuasiHttpProcessingOptions
    {
        public int TimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
    }
}
