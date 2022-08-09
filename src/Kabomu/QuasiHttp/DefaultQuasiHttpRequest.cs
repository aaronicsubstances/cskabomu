using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpRequest"/> interface
    /// in which properties of the interface are mutable.
    /// </summary>
    public class DefaultQuasiHttpRequest : IQuasiHttpRequest
    {
        /// <summary>
        /// Gets or sets the equivalent of path component of HTTP request line.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP request headers.
        /// </summary>
        public IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets the request body.
        /// </summary>
        public IQuasiHttpBody Body { get; set; }

        /// <summary>
        /// Gets or sets an HTTP method value.
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets an HTTP request version value.
        /// </summary>
        public string HttpVersion { get; set; }
    }
}
