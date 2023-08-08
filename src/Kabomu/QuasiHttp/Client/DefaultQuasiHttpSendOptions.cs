using System.Collections.Generic;

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// Implementation of the <see cref="IQuasiHttpSendOptions"/> interface providing mutable versions of
    /// all properties.
    /// </summary>
    public class DefaultQuasiHttpSendOptions : IQuasiHttpSendOptions
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
        /// Gets or sets the value that imposes a maximum size on the headers and chunks which will be generated during
        /// a send request.
        /// </summary>
        public int MaxChunkSize { get; set; }

        /// <summary>
        /// Gets or sets an indication of whether response buffering is enabled or not.
        /// </summary>
        public bool? ResponseBufferingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value that imposes a maximum size on response bodies when they are being buffered in situations
        /// where response streaming is disabled.
        /// </summary>
        public int ResponseBodyBufferingSizeLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether null responses received from sending requests
        /// should result in an error, or should simply be returned as is.
        /// </summary>
        public bool? EnsureNonNullResponse { get; set; }
    }
}