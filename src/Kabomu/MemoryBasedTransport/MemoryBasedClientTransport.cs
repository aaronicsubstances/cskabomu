using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.MemoryBasedTransport
{
    /// <summary>
    /// Implements the standard in-memory client-side quasi http transport provided by the
    /// Kabomu library, which can act both connection-oriented mode and connection bypass modes.
    /// </summary>
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport, IQuasiHttpAltTransport
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public MemoryBasedClientTransport()
        {
        }

        /// <summary>
        /// Gets or sets the endpoint which should identify this instance.
        /// </summary>
        public object LocalEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the virtual hub of servers connected to this instance. Direct request processing
        /// and indirect request proessing via connection allocation are both done through this dependency.
        /// </summary>
        public IMemoryBasedTransportHub Hub { get; set; }

        public Tuple<Task<IQuasiHttpResponse>, object> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectivityParams connectivityParams)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            var resTask = hub.ProcessSendRequest(LocalEndpoint, connectivityParams, request);
            object sendCancellationHandle = null;
            return Tuple.Create(resTask, sendCancellationHandle);
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }

        public Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.AllocateConnection(LocalEndpoint, connectivityParams);
        }

        public Task ReleaseConnection(object connection)
        {
            return MemoryBasedServerTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.ReadBytesInternal(false, connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.WriteBytesInternal(false, connection, data, offset, length);
        }
    }
}
