using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Server
{
    /// <summary>
    /// Implementation of the <see cref="IQuasiHttpProcessingOptions"/> interface providing mutable versions of
    /// all properties.
    /// </summary>
    public class DefaultQuasiHttpProcessingOptions : IQuasiHttpProcessingOptions
    {
        /// <summary>
        /// Gets or sets the wait time period in milliseconds for the processing of a request to succeed. To indicate
        /// forever wait or infinite timeout, use -1 or any negative value. 
        /// </summary>
        public int TimeoutMillis { get; set; }

        /// <summary>
        /// Gets the value that imposes a maximum size on the chunks and read buffers which will be generated during
        /// the processing of a request.
        /// </summary>
        public int MaxChunkSize { get; set; }
    }
}
