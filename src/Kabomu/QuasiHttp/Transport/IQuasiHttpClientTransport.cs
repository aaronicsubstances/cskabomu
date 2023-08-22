using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Kabomu.QuasiHttp.Client;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Equivalent of TCP client socket factory that provides <see cref="Client.StandardQuasiHttpClient"/> instances
    /// with client connections for sending quasi http requests to servers or remote endpoints.
    /// </summary>
    public interface IQuasiHttpClientTransport : IQuasiHttpTransport
    {
        /// <summary>
        /// Creates a connection to a remote endpoint.
        /// </summary>
        /// <param name="remoteEndpoint">the target endpoint of the connection
        /// allocation request</param>
        /// <param name="sendOptions">communication endpoint information. When used with a
        /// <see cref="Client.StandardQuasiHttpClient"/> instance, this will be the result of merging
        /// the default send options of the instance with the particular send options specified in the
        /// Send() method, ie the instance method which initiated the connection allocation request.</param>
        /// <returns>a task whose result will contain a connection ready for use as a duplex
        /// stream of data for reading and writing</returns>
        Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpSendOptions sendOptions);
    }
}
