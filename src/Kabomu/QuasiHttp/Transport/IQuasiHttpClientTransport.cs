using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Equivalent of TCP client socket factory that provides <see cref="Client.StandardQuasiHttpClient"/> instances
    /// with client operations for sending quasi http requests to servers or remote endpoints.
    /// </summary>
    public interface IQuasiHttpClientTransport : IQuasiHttpTransport
    {
        /// <summary>
        /// Creates a connection to a server or remote endpoint.
        /// </summary>
        /// <param name="connectivityParams">communication endpoint information required to connect to a server or
        /// remote endpoint</param>
        /// <returns>a task whose result will contain a connection ready for use as a duplex
        /// stream of data for reading and writing</returns>
        Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams);
    }
}
