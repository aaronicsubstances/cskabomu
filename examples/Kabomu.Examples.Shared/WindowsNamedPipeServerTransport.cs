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
                await Server.AcceptConnection(
                    new DefaultConnectionAllocationResponse
                    {
                        Connection = pipeServer
                    }
                );
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "connection processing error");
            }
        }

        public object GetWriter(object connection)
        {
            return GetWriterInternal(connection);
        }

        public object GetReader(object connection)
        {
            return GetReaderInternal(connection);
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static object GetWriterInternal(object connection)
        {
            return (PipeStream)connection;
        }

        internal static object GetReaderInternal(object connection)
        {
            return (PipeStream)connection;
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var pipeStream = (PipeStream)connection;
            pipeStream.Dispose();
            return Task.CompletedTask;
        }
    }
}
