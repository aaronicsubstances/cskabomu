using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpTransport : IQuasiHttpServerTransport, IQuasiHttpClientTransport, IQuasiHttpTransportBypass
    {
        public Func<IQuasiHttpRequest, IConnectivityParams, Tuple<Task<IQuasiHttpResponse>, object>> ProcessSendRequestCallback { get; set; }
        public Action<object> CancelSendRequestCallback { get; set; }
        public Func<object, IQuasiHttpResponse, Task<bool>> WillCancelSendMakeResponseBodyUnusableCallback { get; set; }
        public Func<IConnectivityParams, Task<IConnectionAllocationResponse>> AllocateConnectionCallback { get; set; }
        public Func<object, Task> ReleaseConnectionCallback { get; set; }
        public Func<object, byte[], int, int, Task<int>> ReadBytesCallback { get; set; }
        public Func<object, byte[], int, int, Task> WriteBytesCallback { get; set; }
        public Func<Task> StartCallback { get;  set; }
        public Func<Task<bool>> IsRunningCallback { get; set; }
        public Func<Task> StopCallback { get; set; }
        public Func<Task<IConnectionAllocationResponse>> ReceiveConnectionCallback { get; set; }
        public IMutexApi MutexApi { get; set; }

        public Tuple<Task<IQuasiHttpResponse>, object> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectivityParams connectivityParams)
        {
            return ProcessSendRequestCallback.Invoke(request, connectivityParams);
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            CancelSendRequestCallback.Invoke(sendCancellationHandle);
        }

        public Task<bool> WillCancelSendMakeResponseBodyUnusable(object sendCancellationHandle, IQuasiHttpResponse response)
        {
            return WillCancelSendMakeResponseBodyUnusableCallback.Invoke(sendCancellationHandle, response);
        }

        public Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            return AllocateConnectionCallback.Invoke(connectivityParams);
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
            return ReceiveConnectionCallback.Invoke();
        }

        public Task Start()
        {
            return StartCallback.Invoke();
        }

        public Task<bool> IsRunning()
        {
            return IsRunningCallback.Invoke();
        }

        public Task Stop()
        {
            return StopCallback.Invoke();
        }
    }
}
