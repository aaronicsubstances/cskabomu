using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Represents the equivalent of an HTTP response entity: response status line,
    /// response headers, and response body.
    /// </summary>
    public interface IQuasiHttpResponse : ICustomDisposable
    {
        /// <summary>
        /// Gets the equivalent of HTTP status line and response headers.
        /// </summary>
        IQuasiHttpResponseHeaderPart Headers { get; }

        /// <summary>
        /// Gets the response body.
        /// </summary>
        object Body { get; }

        /// Gets any objects which may be of interest during response processing.
        IDictionary<string, object> Environment { get; }
    }
}
