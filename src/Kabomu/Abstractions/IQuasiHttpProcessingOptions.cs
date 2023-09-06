using System.Collections.Generic;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Used to configure parameters which affect processing quasi http requests
    /// and responses.
    /// </summary>
    public interface IQuasiHttpProcessingOptions
    {
        /// <summary>
        /// Gets any extra information which can help a transport to locate a communication endpoint.
        /// </summary>
        IDictionary<string, object> ExtraConnectivityParams { get; set; }

        /// <summary>
        /// Gets the wait time period in milliseconds for a send request to succeed. To indicate
        /// forever wait or infinite timeout, use -1 or any negative value. 
        /// </summary>
        /// <remarks>
        /// Note that value of zero will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used (including the possibility of infinite timeout).
        /// </remarks>
        int TimeoutMillis { get; set; }

        /// <summary>
        /// Gets the value that imposes a maximum size on the headers of requests and
        /// responses which will be encountered during sending out requests and
        /// receipt of responses.
        /// </summary>
        /// <remarks>
        /// Note that zero and negative values will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int MaxHeadersSize { get; set; }

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
        bool? ResponseBufferingEnabled { get; set; }

        /// <summary>
        /// Gets the value that imposes a maximum size on response bodies when they are being buffered,
        /// i.e. in situations where response buffering is enabled.
        /// </summary>
        /// <remarks>
        /// Note that zero and negative values will be interpreted as unspecified, and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int ResponseBodyBufferingSizeLimit { get; set; }
    }
}