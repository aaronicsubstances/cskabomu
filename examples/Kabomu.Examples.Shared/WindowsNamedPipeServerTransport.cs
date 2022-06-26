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
        private readonly object _lock = new object();
        private readonly string _path;
        private CancellationTokenSource _startCancellationHandle;

        public WindowsNamedPipeServerTransport(string path)
        {
            _path = path;
        }

        public Task Start()
        {
            lock (_lock)
            {
                if (_startCancellationHandle == null)
                {
                    _startCancellationHandle = new CancellationTokenSource();
                }
            }
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            lock (_lock)
            {
                _startCancellationHandle?.Cancel();
                _startCancellationHandle = null;
            }
            return Task.CompletedTask;
        }

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _startCancellationHandle != null;
                }
            }
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            NamedPipeServerStream pipeServer;
            Task waitTask;
            lock (_lock)
            {
                if (_startCancellationHandle == null)
                {
                    throw new Exception("transport not started");
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
            return WindowsNamedPipeClientTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return WindowsNamedPipeClientTransport.ReadBytesInternal(connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WindowsNamedPipeClientTransport.WriteBytesInternal(connection, data, offset, length);
        }
    }
}
