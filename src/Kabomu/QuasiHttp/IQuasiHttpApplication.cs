using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Represents a quasi http request processing function used by <see cref="Server.StandardQuasiHttpServer"/> instances
    /// to generate quasi http responses.
    /// </summary>
    public interface IQuasiHttpApplication
    {
        /// <summary>
        /// Processes a quasi htp request.
        /// </summary>
        /// <param name="request">the quasi http request</param>
        /// <returns>a task whose result will be the response to the quasi http request</returns>
        Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request);
    }
}
