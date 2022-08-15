﻿using Kabomu.QuasiHttp.EntityBody;
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
        /// Gets or sets the equivalent of request target component of HTTP request line.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP request headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, headers are case sensitive. Also setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the request body.
        /// </remarks>
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
