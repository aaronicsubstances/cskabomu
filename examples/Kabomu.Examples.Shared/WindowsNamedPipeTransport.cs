using Kabomu.Common;
using Kabomu.Common.Transports;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeTransport : IQuasiHttpTransport
    {
        private readonly string _path;
        private readonly CancellationTokenSource _startCancelled;

        public WindowsNamedPipeTransport(string path)
        {
            _path = path;
            _startCancelled = new CancellationTokenSource();
        }

        public bool DirectSendRequestProcessingEnabled => false;

        public async Task Start()
        {
        }

        public async Task Stop()
        {
            _startCancelled.Cancel();
        }

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            throw new NotImplementedException();
        }

        public async Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            var path = (string)connectionRequest.RemoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync(_startCancelled.Token);
            return pipeClient;
        }

        public async Task ReleaseConnection(object connection)
        {
            var pipeStream = (PipeStream)connection;
            pipeStream.Dispose();
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (PipeStream)connection;
            return networkStream.WriteAsync(data, offset, length);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (PipeStream)connection;
            return networkStream.ReadAsync(data, offset, length);
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            var pipeServer = new NamedPipeServerStream(_path, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await pipeServer.WaitForConnectionAsync(_startCancelled.Token);
            return new DefaultConnectionAllocationResponse
            {
                Connection = pipeServer
            };
        }
    }
}
