using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Serves to provide communication endpoint identifiers, which can provide instances of
    /// <see cref="IQuasiHttpClientTransport"/> interface enough information to allocate connections.
    /// </summary>
    /// <remarks>
    /// Note that the interpretation of the properties and values of instances are strictly specific
    /// to the quasi http transport which will use them.
    /// </remarks>
    public interface IConnectivityParams
    {
        /// <summary>
        /// Gets an object which can almost single-handedly identify a communication endpoint. E.g. a TCP port,
        /// an HTTP URL.
        /// </summary>
        object RemoteEndpoint { get; }

        /// <summary>
        /// Gets a collection of key-value pairs which together help to connect to a communication endpoint. E.g.
        /// a URL scheme such as "https" by itself may not be needed to identify a communication endpoint located
        /// at host "www.google.com", but is required to connect to www.google.com using TLS protocol.
        /// </summary>
        IDictionary<string, object> ExtraParams { get; }
    }
}
