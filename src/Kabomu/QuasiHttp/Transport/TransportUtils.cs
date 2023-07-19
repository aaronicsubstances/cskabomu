using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Contains standard names for common keys which may used in
    /// environments of quasi requests and responses.
    /// </summary>
    public static class TransportUtils
    {
        /// <summary>
        /// Request environment variable name of "kabomu.local_peer_endpoint" for local server endpoint.
        /// </summary>
        public static readonly string ReqEnvKeyLocalPeerEndpoint = "kabomu.local_peer_endpoint";

        /// <summary>
        /// Request environment variable name of "kabomu.remote_peer_endpoint" for remote client endpoint.
        /// </summary>
        public static readonly string ReqEnvKeyRemotePeerEndpoint = "kabomu.remote_peer_endpoint";

        /// <summary>
        /// Response environment variable of "kabomu.response_buffering_enabled" for indicating whether or not response has been bufferred already.
        /// </summary>
        public static readonly string ResEnvKeyResponseBufferingApplied = "kabomu.response_buffering_enabled";

        /// <summary>
        /// Environment variable for the transport instance from which a request was received.
        /// </summary>
        public static readonly string ReqEnvKeyTransportInstance = "kabomu.transport";

        /// <summary>
        /// Environment variable for the connection from which a request was received.
        /// </summary>
        public static readonly string ReqEnvKeyConnection = "kabomu.connection";
    }
}
