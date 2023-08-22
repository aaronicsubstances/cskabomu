using System.Collections.Generic;

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// Used to configure send requests to <see cref="StandardQuasiHttpClient"/> instances.
    /// </summary>
    public interface IQuasiHttpSendOptions
    {
        /// <summary>
        /// Gets any extra information which can help a transport to locate a communication endpoint.
        /// </summary>
        IDictionary<string, object> ExtraConnectivityParams { get; }

        /// <summary>
        /// Gets the wait time period in milliseconds for a send request to succeed. To indicate
        /// forever wait or infinite timeout, use -1 or any negative value. 
        /// </summary>
        /// <remarks>
        /// Note that value of zero will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used (including the possibility of infinite timeout).
        /// </remarks>
        int TimeoutMillis { get; }

        /// <summary>
        /// Gets the value that imposes a maximum size on the headers and chunks which will be generated during
        /// a send request, according to the chunked transfer protocol.
        /// </summary>
        /// <remarks>
        /// Note that zero and negative values will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int MaxChunkSize { get; }

        /// <summary>
        /// Gets the indication of whether response buffering is enabled or not.
        /// <para></para>
        /// A value of false means clients are responsible for closing a response if it has a body.
        /// <para></para>
        /// A value of true means send request processing must ensure that responses are released before returning them to clients,
        /// by generating equivalent responses with buffered bodies.
        /// <para></para>
        /// Else a value of null means it is unspecified whether response buffering is enabled or not, and in the absence of
        /// any overriding options a client-specific default action will be taken.
        /// </summary>
        bool? ResponseBufferingEnabled { get; }

        /// <summary>
        /// Gets the value that imposes a maximum size on response bodies when they are being buffered,
        /// i.e. in situations where response buffering is enabled.
        /// </summary>
        /// <remarks>
        /// Note that zero and negative values will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int ResponseBodyBufferingSizeLimit { get; }

        /// <summary>
        /// Indicates whether null responses received from sending requests
        /// should result in an error, or should simply be returned as is.
        /// </summary>
        bool? EnsureNonNullResponse { get; }
    }
}