using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
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
        private readonly string _path;
        private CancellationTokenSource _startCancellationHandle;

        public WindowsNamedPipeServerTransport(string path)
        {
            _path = path;
            MutexApi = new LockBasedMutexApi();
        }

        public IMutexApi MutexApi { get; set; }

        public async Task Start()
        {
            using (await MutexApi.Synchronize())
            {
                if (_startCancellationHandle == null)
                {
                    _startCancellationHandle = new CancellationTokenSource();
                }
            }
        }

        public async Task Stop()
        {
            using (await MutexApi.Synchronize())
            {
                _startCancellationHandle?.Cancel();
                _startCancellationHandle = null;
            }
        }

        public async Task<bool> IsRunning()
        {
            using (await MutexApi.Synchronize())
            {
                return _startCancellationHandle != null;
            }
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            NamedPipeServerStream pipeServer;
            Task waitTask;
            using (await MutexApi.Synchronize())
            {
                if (_startCancellationHandle == null)
                {
                    throw new TransportNotStartedException();
                }
                pipeServer = new NamedPipeServerStream(_path, PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                waitTask = pipeServer.WaitForConnectionAsync(_startCancellationHandle.Token);
            }
            await waitTask;
            return new DefaultConnectionAllocationResponse
            {
                Connection = pipeServer
            };
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
