using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IQuasiHttpTransport
    {
        Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest);
        Task ReleaseConnection(object connection);
        Task WriteBytes(object connection, byte[] data, int offset, int length);
        Task<int> ReadBytes(object connection, byte[] data, int offset, int length);
        Task<IConnectionAllocationResponse> ReceiveConnection();

        /// <summary>
        /// Memory-based transports return true with a probability between 0 and 1,
        /// in order to catch any hidden errors during serialization to bytes.
        /// HTTP-based transports return true always in order to completely take over
        /// processing of (Quasi) HTTP requests.
        /// </summary>
        bool DirectSendRequestProcessingEnabled { get; }

        Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo);
        Task Start();
        Task Stop();
    }
}
