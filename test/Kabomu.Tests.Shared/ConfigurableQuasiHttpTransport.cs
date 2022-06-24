using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpTransport : IQuasiHttpTransport
    {
        public bool DirectSendRequestProcessingEnabled { get; set; }

        public Func<IQuasiHttpRequest, IConnectionAllocationRequest, Task<IQuasiHttpResponse>> ProcessSendRequestCallback { get; set; }

        public Func<IConnectionAllocationRequest, Task<object>> AllocateConnectionCallback { get; set; }

        public Func<object, Task> ReleaseConnectionCallback { get; set; }

        public Func<object, byte[], int, int, Task<int>> ReadBytesCallback { get; set; }

        public Func<object, byte[], int, int, Task> WriteBytesCallback { get; set; }

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            return ProcessSendRequestCallback.Invoke(request, connectionAllocationInfo);
        }

        public Task<object> AllocateConnection(IConnectionAllocationRequest connectionAllocationRequest)
        {
            return AllocateConnectionCallback.Invoke(connectionAllocationRequest);
        }

        public async Task ReleaseConnection(object connection)
        {
            if (ReleaseConnectionCallback != null)
            {
                await ReleaseConnectionCallback.Invoke(connection);
            }
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesCallback.Invoke(connection, data, offset, length);
        }

        public async Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            if (WriteBytesCallback != null)
            {
                await WriteBytesCallback.Invoke(connection, data, offset, length);
            }
        }

        public Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            throw new NotImplementedException();
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }
    }
}
