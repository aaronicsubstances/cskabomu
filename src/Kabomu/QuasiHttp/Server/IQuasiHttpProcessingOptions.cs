using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Server
{
    /// <summary>
    /// Used to configure request processing by <see cref="StandardQuasiHttpServer"/> instances.
    /// </summary>
    public interface IQuasiHttpProcessingOptions
    {
        /// <summary>
        /// Gets the wait time period in milliseconds for the processing of a request to succeed. To indicate
        /// forever wait or infinite timeout, use -1 or any negative value.
        /// </summary>
        /// <remarks>
        /// Note that value of zero will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used (including the possibility of infinite timeout).
        /// </remarks>
        int TimeoutMillis { get; set; }

        /// <summary>
        /// Gets the value that imposes a maximum size on the headers and chunks which will be generated during
        /// the processing of a request, in accordance with the chunked transfer protocol.
        /// </summary>
        /// <remarks>
        /// Note that zero and negative values will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int MaxChunkSize { get; set; }
    }
}
