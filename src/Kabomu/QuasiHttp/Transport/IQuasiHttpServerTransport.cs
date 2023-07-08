using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Equivalent of TCP server sockets that provides <see cref="Server.StandardQuasiHttpServer"/> instances
    /// with server operations for sending quasi http requests to servers or remote endpoints.
    /// </summary>
    public interface IQuasiHttpServerTransport : IQuasiHttpTransport
    {
    }
}
