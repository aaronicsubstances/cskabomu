using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System.Threading.Tasks;

namespace Kabomu.Tests.Internals
{
    class TestClientTransport : IQuasiHttpClientTransport
    {
        public static readonly string ConnectivityParamAllocateDelayMillis = "allocate.delay";

        public IEventLoopApi EventLoopApi { get; set; }

        public Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            var testConnection = (TestConnection)connectivityParams.RemoteEndpoint;
            var allocateDelayMillis = 0;
            if (connectivityParams.ExtraParams?.ContainsKey(ConnectivityParamAllocateDelayMillis) ?? false)
            {
                allocateDelayMillis = (int)connectivityParams.ExtraParams[ConnectivityParamAllocateDelayMillis];
            }
            var result = new DefaultConnectionAllocationResponse
            {
                Connection = testConnection
            };
            // create tcs synchronously.
            var tcs = new TaskCompletionSource<IConnectionAllocationResponse>();
            EventLoopApi.SetTimeout(() => tcs.SetResult(result), allocateDelayMillis);
            return tcs.Task;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var testConnection = (TestConnection)connection;
            int bytesRead = testConnection.InputStream.Read(data, offset, length);
            // create tcs synchronously.
            var tcs = new TaskCompletionSource<int>();
            EventLoopApi.SetTimeout(() => tcs.SetResult(bytesRead), testConnection.ReadDelayMillis);
            return tcs.Task;
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            var testConnection = (TestConnection)connection;
            testConnection.OutputStream.Write(data, offset, length);
            // create tcs synchronously.
            var tcs = new TaskCompletionSource<object>();
            EventLoopApi.SetTimeout(() => tcs.SetResult(null), testConnection.WriteDelayMillis);
            return tcs.Task;
        }

        public Task ReleaseConnection(object connection)
        {
            var testConnection = (TestConnection)connection;
            testConnection.InputStream.Dispose();
            testConnection.OutputStream.Dispose();
            // create tcs synchronously.
            var tcs = new TaskCompletionSource<object>();
            EventLoopApi.SetTimeout(() => tcs.SetResult(null), testConnection.ReleaseDelayMillis);
            return tcs.Task;
        }
    }
}
