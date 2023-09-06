using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Represents the equivalent of an HTTP request entity: request line,
    /// request headers, and request body.
    /// </summary>
    public interface IQuasiHttpRequest : ICustomDisposable
    {
        /// <summary>
        /// Gets the equivalent of HTTP request line and headers.
        /// </summary>
        IQuasiHttpRequestHeaderPart Headers { get; }

        /// <summary>
        /// Gets the request body.
        /// </summary>
        object Body { get; }

        /// Gets any objects which may be of interest during request processing.
        IDictionary<string, object> Environment { get; }
    }
}
