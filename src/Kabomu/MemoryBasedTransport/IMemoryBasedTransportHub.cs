using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.MemoryBasedTransport
{
    /// <summary>
    /// Represents a virtual in-memory computer networking hub attached to instances
    /// of the <see cref="IQuasiHttpServer"/> type.
    /// </summary>
    /// <remarks>
    /// This interface serves as a backbone for in-memory computer networking.
    /// <para></para>
    /// Implementations are expected to impose restrictions on the acceptables types of attached servers.
    /// </remarks>
    public interface IMemoryBasedTransportHub
    {
        /// <summary>
        /// Associates a quasi http server with an endpoint. Separation of endpoint from server enables
        /// multiple endpoints to be associated to a single server.
        /// </summary>
        /// <param name="endpoint">the endpoint associated with this server.</param>
        /// <param name="server">the server associated with the endpoint</param>
        /// <returns>task representing the asynchronous add operation</returns>
        Task AddServer(object endpoint, IQuasiHttpServer server);

        /// <summary>
        /// Processes a send request to an attached server on behalf of clients.
        /// </summary>
        /// <param name="clientEndpoint">the endpoint identifying the client making the send request.</param>
        /// <param name="connectivityParams">endpoint information identifying the attached server</param>
        /// <param name="request">the request to be processed.</param>
        /// <returns>a task whose result will be the response from an attached server to the request from the client.</returns>
        Task<IQuasiHttpResponse> ProcessSendRequest(object clientEndpoint,
            IConnectivityParams connectivityParams, IQuasiHttpRequest request);

        /// <summary>
        /// Allocates a connection to an attached server on behalf of clients.
        /// </summary>
        /// <param name="clientEndpoint">the endpoint identifying the client making the send request.</param>
        /// <param name="connectivityParams">endpoint information identifying the attached server</param>
        /// <returns>a task whose result will contain a newly allocated connection from the request client
        /// to an attached server</returns>
        Task<IConnectionAllocationResponse> AllocateConnection(object clientEndpoint, 
            IConnectivityParams connectivityParams);
    }
}
