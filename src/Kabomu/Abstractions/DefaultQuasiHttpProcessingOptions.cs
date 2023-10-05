using System.Collections.Generic;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Provides default implementation of the <see cref="IQuasiHttpProcessingOptions"/>
    /// interface.
    /// </summary>
    public class DefaultQuasiHttpProcessingOptions : IQuasiHttpProcessingOptions
    {
        /// <summary>
        /// Gets or sets any extra information which can help a transport to locate a communication endpoint.
        /// </summary>
        public IDictionary<string, object> ExtraConnectivityParams { get; set; }

        /// <summary>
        /// Gets or sets the wait time period in milliseconds for a send request to succeed. To indicate
        /// forever wait or infinite timeout, use -1 or any negative value. 
        /// </summary>
        public int TimeoutMillis { get; set; }

        /// <summary>
        /// Gets or sets the value that imposes a maximum size on the headers of requests and
        /// responses which will be encountered during sending out requests and
        /// receipt of responses.
        /// </summary>
        public int MaxHeadersSize { get; set; }

        /// <summary>
        /// Gets or sets a value that imposes a maximum size on response bodies.
        /// </summary>
        public int MaxResponseBodySize { get; set; }
    }
}