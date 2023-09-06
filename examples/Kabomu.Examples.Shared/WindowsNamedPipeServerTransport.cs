using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeServerTransport : IQuasiHttpServerTransport
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly string _path;
        private readonly CancellationTokenSource _startCancellationHandle;

        public WindowsNamedPipeServerTransport(string path)
        {
            _path = path;
            _startCancellationHandle = new CancellationTokenSource();
        }

        public StandardQuasiHttpServer Server { get; set; }
        public IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }

        public Task Start()
        {
            // don't wait.
            _ = AcceptConnections();
            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            _startCancellationHandle.Cancel();
            await Task.Delay(1_000);
        }

        private async Task AcceptConnections()
        {
            try
            {
                while (true)
                {
                    var pipeServer = new NamedPipeServerStream(_path, PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await pipeServer.WaitForConnectionAsync(_startCancellationHandle.Token);
                    // don't wait.
                    _ = ReceiveConnection(pipeServer);
                }
            }
            catch (Exception e)
            {
                if (_startCancellationHandle.IsCancellationRequested)
                {
                    LOG.Info("connection accept ended");
                }
                else
                {
                    LOG.Warn(e, "connection accept error");
                }
            }
        }

        private async Task ReceiveConnection(NamedPipeServerStream pipeServer)
        {
            try
            {
                var connection = new DuplexStreamConnection(pipeServer, false,
                    DefaultProcessingOptions);
                await Server.AcceptConnection(connection);
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "connection processing error");
            }
        }

        public object GetWriter(IQuasiTcpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Writer;
        }

        public object GetReader(IQuasiTcpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Reader;
        }

        public Task ReleaseConnection(IQuasiTcpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Release();
        }

        public Task Write(IQuasiTcpConnection connection, bool isResponse,
            byte[] encodedHeaders, object requestBodyReader)
        {
            return ((DuplexStreamConnection)connection).Write(isResponse,
                encodedHeaders, requestBodyReader);
        }

        public Task<IEncodedReadRequest> Read(
            IQuasiTcpConnection connection,
            bool isResponse)
        {
            return ((DuplexStreamConnection)connection).Read(
                isResponse);
        }
    }
}
