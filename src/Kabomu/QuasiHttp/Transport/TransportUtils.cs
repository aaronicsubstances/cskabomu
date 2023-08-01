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
        /// Request environment variable for local server endpoint.
        /// </summary>
        public static readonly string ReqEnvKeyLocalPeerEndpoint = "kabomu.local_peer_endpoint";

        /// <summary>
        /// Request environment variable for remote client endpoint.
        /// </summary>
        public static readonly string ReqEnvKeyRemotePeerEndpoint = "kabomu.remote_peer_endpoint";

        /// <summary>
        /// Request environment variable for the transport instance from which a request was received.
        /// </summary>
        public static readonly string ReqEnvKeyTransportInstance = "kabomu.transport";

        /// <summary>
        /// Request environment variable for the connection from which a request was received.
        /// </summary>
        public static readonly string ReqEnvKeyConnection = "kabomu.connection";

        /// <summary>
        /// Response environment variable for indicating whether or not response has been bufferred already.
        /// </summary>
        public static readonly string ResEnvKeyResponseBufferingApplied = "kabomu.response_buffering_enabled";

        /// <summary>
        /// Response environment variable for indicating that response should not be sent at all. Intended
        /// for use in responding to fire and forget requests.
        /// </summary>
        public static readonly string ResEnvKeySkipResponseSending = "kabomu.skip_response_sending";

        /// <summary>
        /// Connectivity parameter for indicating to client transports that it
        /// can create connections which provide empty reads, because such
        /// connections are to be used in situations where responses are not needed,
        /// or where responses won't arrive at all.
        /// </summary>
        public static readonly string ConnectivityParamFireAndForget = "kabomu.fire_and_forget";
    }
}
