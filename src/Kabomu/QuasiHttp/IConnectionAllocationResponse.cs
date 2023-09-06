using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Contains connection-related information created by
    /// <see cref="IQuasiHttpServerTransport"/> or <see cref="IQuasiHttpClientTransport"/>
    /// instances, in response to a connection allocation or receive request.
    /// </summary>
    public interface IConnectionAllocationResponse
    {
        /// <summary>
        /// Stores any objects which a quasi http transport will need
        /// to respond to reads and writes from instances.
        /// </summary>
        object Connection { get; }

        IQuasiHttpProcessingOptions ProcessingOptions { get; }

        /// <summary>
        /// Gets any environment variables associated with a connection received from a
        /// quasi http transport.
        /// </summary>
        IDictionary<string, object> Environment { get; }
    }
}
