﻿using Kabomu.QuasiHttp.Transport;
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
        private readonly object _mutex = new object();
        private readonly string _path;
        private CancellationTokenSource _startCancellationHandle;

        public WindowsNamedPipeServerTransport(string path)
        {
            _path = path;
        }

        public Task Start()
        {
            lock (_mutex)
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
            lock (_mutex)
            {
                _startCancellationHandle?.Cancel();
                _startCancellationHandle = null;
            }
            return Task.CompletedTask;
        }

        public bool IsRunning()
        {
            lock (_mutex)
            {
                return _startCancellationHandle != null;
            }
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            NamedPipeServerStream pipeServer;
            Task waitTask;
            lock (_mutex)
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
