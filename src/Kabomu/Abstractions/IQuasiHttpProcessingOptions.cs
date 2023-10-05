using System.Collections.Generic;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Used to configure parameters which affect processing of quasi http requests
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
        /// Note that zero and negative values will be interpreted as unspecified,
        /// and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int MaxHeadersSize { get; set; }

        /// <summary>
        /// Gets the value that imposes a maximum size on response bodies. To indicate absence
        /// of a limit, use -1 or any negative value.
        /// </summary>
        /// <remarks>
        /// Note that zero will be interpreted as unspecified,
        /// and in the absence of any overriding options
        /// a client-specific default value will be used.
        /// </remarks>
        int MaxResponseBodySize { get; set; }
    }
}