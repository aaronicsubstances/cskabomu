using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Contains connection-related information created by
    /// <see cref="IQuasiHttpClientTransport"/> instances,
    /// or acceptable by <see cref="IQuasiHttpServerTransport"/>
    /// instances.
    /// </summary>
    public interface IQuasiHttpConnection
    {
        CancellationToken CancellationToken { get; }
        IQuasiHttpProcessingOptions ProcessingOptions { get; }

        /// <summary>
        /// Gets any environment variables associated with a connection received from a
        /// quasi http transport.
        /// </summary>
        IDictionary<string, object> Environment { get; }
    }
}
