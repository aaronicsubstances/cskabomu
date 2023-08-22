using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// Represents result of sending quasi http requests with
    /// <see cref="StandardQuasiHttpClient"/> and
    /// <see cref="IQuasiHttpAltTransport"/> instances.
    /// </summary>
    public struct QuasiHttpSendResponse
    {
        public Task<IQuasiHttpResponse> ResponseTask { get; set; }
        public object CancellationHandle { get; set; }
    }
}
