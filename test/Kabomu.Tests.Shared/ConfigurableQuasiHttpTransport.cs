using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpTransport : IQuasiHttpClientTransport, IQuasiHttpServerTransport
    {
        public Func<Task<bool>> CanProcessSendRequestDirectlyCallback { get; set; }

        public Func<IQuasiHttpRequest, IConnectionAllocationRequest, Task<IQuasiHttpResponse>> ProcessSendRequestCallback { get; set; }

        public Func<IConnectionAllocationRequest, Task<object>> AllocateConnectionCallback { get; set; }

        public Func<object, Task> ReleaseConnectionCallback { get; set; }

        public Func<object, byte[], int, int, Task<int>> ReadBytesCallback { get; set; }

        public Func<object, byte[], int, int, Task> WriteBytesCallback { get; set; }

        public IMutexApi MutexApi { get; set; }

        public Task<bool> CanProcessSendRequestDirectly()
        {
            return CanProcessSendRequestDirectlyCallback.Invoke();
        }

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            return ProcessSendRequestCallback.Invoke(request, connectionAllocationInfo);
        }

        public Task<object> AllocateConnection(IConnectionAllocationRequest connectionAllocationRequest)
        {
            return AllocateConnectionCallback.Invoke(connectionAllocationRequest);
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionCallback.Invoke(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesCallback.Invoke(connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesCallback.Invoke(connection, data, offset, length);
        }

        public Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            throw new NotImplementedException();
        }

        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsRunning()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }
    }
}
