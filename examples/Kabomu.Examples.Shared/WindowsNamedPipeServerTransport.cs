using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
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
        private readonly object _mutex = new object();
        private readonly string _path;
        private CancellationTokenSource _startCancellationHandle;

        public WindowsNamedPipeServerTransport(string path)
        {
            _path = path;
        }

        public StandardQuasiHttpServer Server { get; set; }

        public Task Start()
        {
            lock (_mutex)
            {
                if (_startCancellationHandle == null)
                {
                    _startCancellationHandle = new CancellationTokenSource();
                    _ = ServerUtils.AcceptConnections(
                        ReceiveConnection, IsDoneRunning);
                }
            }
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            lock (_mutex)
            {
                _startCancellationHandle?.Cancel();
                _startCancellationHandle = null;
            }
            return Task.CompletedTask;
        }

        private Task<bool> IsDoneRunning(Exception latestError)
        {
            if (latestError != null)
            {
                LOG.Warn(latestError, "connection receive error");
                return Task.FromResult(false);
            }
            lock (_mutex)
            {
                return Task.FromResult(_startCancellationHandle != null);
            }
        }

        private async Task<bool> ReceiveConnection()
        {
            NamedPipeServerStream pipeServer;
            Task waitTask;
            lock (_mutex)
            {
                pipeServer = new NamedPipeServerStream(_path, PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                waitTask = pipeServer.WaitForConnectionAsync(_startCancellationHandle.Token);
            }
            await waitTask;
            var c = new DefaultConnectionAllocationResponse
            {
                Connection = pipeServer
            };
            await Server.AcceptConnection(c);
            return true;
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var pipeStream = (PipeStream)connection;
            pipeStream.Dispose();
            return Task.CompletedTask;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(connection, data, offset, length);
        }

        internal static Task<int> ReadBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (PipeStream)connection;
            return networkStream.ReadAsync(data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(connection, data, offset, length);
        }

        internal static Task WriteBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (PipeStream)connection;
            return networkStream.WriteAsync(data, offset, length);
        }
    }
}
