using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeClientTransport : IQuasiHttpClientTransport
    {
        public async Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            var connection = new DuplexStreamConnection(pipeClient, true,
                sendOptions, DefaultSendOptions);

            var mainTask = pipeClient.ConnectAsync();
            try
            {
                await MiscUtils.CompleteMainTask(mainTask, connection.TimeoutId?.Task);
            }
            catch (Exception)
            {
                try
                {
                    // don't wait.
                    _ = connection.Release();
                }
                catch (Exception) { } //ignore
                throw;
            }
            return connection;
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public Task ReleaseConnection(IQuasiHttpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Release();
        }

        public Task Write(IQuasiHttpConnection connection, bool isResponse,
            IEncodedQuasiHttpEntity entity)
        {
            return ((DuplexStreamConnection)connection).Write(isResponse,
                entity);
        }

        public Task<IEncodedQuasiHttpEntity> Read(
            IQuasiHttpConnection connection,
            bool isResponse)
        {
            return ((DuplexStreamConnection)connection).Read(
                isResponse);
        }
    }
}
