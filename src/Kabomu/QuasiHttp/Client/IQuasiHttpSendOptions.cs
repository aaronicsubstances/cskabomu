using System.Collections.Generic;

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// Used to configure send requests to <see cref="IQuasiHttpClient"/> instances.
    /// </summary>
    public interface IQuasiHttpSendOptions
    {
        /// <summary>
        /// Gets any extra information which can help a transport to locate a communication endpoint.
        /// Equivalent to <see cref="Transport.IConnectivityParams.ExtraParams"/> property.
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
        /// Gets the value that imposes a maximum size on the chunks and read buffers which will be generated during
        /// a send request.
        /// </summary>
        /// <remarks>
        /// Note that zero and negative values will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int MaxChunkSize { get; }

        /// <summary>
        /// Gets the indication of whether response streaming is enabled or not.
        /// <para></para>
        /// A value of true means clients are responsible for closing a response if it has a body.
        /// <para></para>
        /// A value of false means send request processing must ensure that responses are closed before returning them to clients,
        /// by generating equivalent responses with buffered bodies.
        /// <para></para>
        /// Else a value of null means it is unspecified whether response streaming is enabled or not, and in the absence of
        /// any overriding options a client-specific default value will be used.
        /// </summary>
        bool? ResponseStreamingEnabled { get; }

        /// <summary>
        /// Gets the value that imposes a maximum size on response bodies when they are being buffered in situations
        /// where response streaming is disabled.
        /// </summary>
        /// <remarks>
        /// Note that zero and negative values will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int ResponseBodyBufferingSizeLimit { get; }
    }
}