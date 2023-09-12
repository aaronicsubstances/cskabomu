using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents a quasi http request processing function used by <see cref="StandardQuasiHttpServer"/> instances
    /// to generate quasi http responses.
    /// </summary>
    public delegate Task<IQuasiHttpResponse> QuasiHttpApplication(
        IQuasiHttpRequest request);
}
