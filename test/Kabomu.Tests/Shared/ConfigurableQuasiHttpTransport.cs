using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpTransport : IQuasiHttpServerTransport, IQuasiHttpClientTransport, IQuasiHttpAltTransport
    {
        public Func<IQuasiHttpRequest, IConnectivityParams, (Task<IQuasiHttpResponse>, object)> ProcessSendRequestCallback { get; set; }
        public Action<object> CancelSendRequestCallback { get; set; }
        public Func<IConnectivityParams, Task<IConnectionAllocationResponse>> AllocateConnectionCallback { get; set; }
        public Func<object, Task> ReleaseConnectionCallback { get; set; }
        public Func<object, byte[], int, int, Task<int>> ReadBytesCallback { get; set; }
        public Func<object, byte[], int, int, Task> WriteBytesCallback { get; set; }
        public Func<Task> StartCallback { get;  set; }
        public Func<bool> IsRunningCallback { get; set; }
        public Func<Task> StopCallback { get; set; }
        public Func<Task<IConnectionAllocationResponse>> ReceiveConnectionCallback { get; set; }
        public Func<object, byte[], IQuasiHttpBody, Task<bool>> TrySerializeBodyCallback { get; set; }
        public Func<object, long, Task<IQuasiHttpBody>> DeserializeBodyCallback { get; set; }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(IQuasiHttpRequest request,
            IConnectivityParams connectivityParams)
        {
            return ProcessSendRequestCallback.Invoke(request, connectivityParams);
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            CancelSendRequestCallback.Invoke(sendCancellationHandle);
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

        public bool IsRunning()
        {
            return IsRunningCallback.Invoke();
        }

        public Task Stop()
        {
            return StopCallback.Invoke();
        }

        public Task<bool> TrySerializeBody(object connection, byte[] prefix, IQuasiHttpBody body)
        {
            return TrySerializeBodyCallback.Invoke(connection, prefix, body);
        }

        public Task<IQuasiHttpBody> DeserializeBody(object connection, long contentLength)
        {
            return DeserializeBodyCallback.Invoke(connection, contentLength);
        }
    }
}
