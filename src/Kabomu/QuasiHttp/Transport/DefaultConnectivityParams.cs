using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IConnectivityParams"/> interface
    /// in which properties of the interface are mutable.
    /// </summary>
    public class DefaultConnectivityParams : IConnectivityParams
    {
        /// <summary>
        /// Gets or sets an object which can almost single-handedly identify a communication endpoint. E.g. a TCP port,
        /// an HTTP URL.
        /// </summary>
        public object RemoteEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a collection of key-value pairs which together help to connect to a communication endpoint. E.g.
        /// a URL scheme such as "https".
        /// </summary>
        public IDictionary<string, object> ExtraParams { get; set; }
    }
}
