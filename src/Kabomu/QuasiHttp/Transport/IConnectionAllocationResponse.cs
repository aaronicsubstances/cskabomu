using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Contains connection and other connection-related information created by a
    /// <see cref="IQuasiHttpServerTransport"/> or <see cref="IQuasiHttpClientTransport"/> instance,
    /// in response to a connection allocation or receive request.
    /// </summary>
    public interface IConnectionAllocationResponse
    {
        /// <summary>
        /// Gets the connection object created by a quasi http transport.
        /// </summary>
        object Connection { get; }

        /// <summary>
        /// Gets any environment variables associated with a connection received from a
        /// quasi http transport.
        /// </summary>
        IDictionary<string, object> Environment { get; }
    }
}
