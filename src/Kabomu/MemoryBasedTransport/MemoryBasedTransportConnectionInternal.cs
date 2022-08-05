using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.MemoryBasedTransport
{
    internal class MemoryBasedTransportConnectionInternal
    {
        private readonly ICancellationHandle _connectionCancellationHandle = new DefaultCancellationHandle();
        private readonly MemoryPipeBackedBody _serverPipe;
        private readonly MemoryPipeBackedBody _clientPipe;

        public MemoryBasedTransportConnectionInternal(IMutexApi serverMutex, IMutexApi clientMutex,
            int serverMaxWriteBufferLimit, int clientMaxWriteBufferLimit)
        {
            _serverPipe = new MemoryPipeBackedBody
            {
                MaxWriteBufferLimit = TransportUtils.DefaultDataBufferLimit
            };
            if (serverMutex != null)
            {
                _serverPipe.MutexApi = serverMutex;
            }
            _clientPipe = new MemoryPipeBackedBody
            {
                MaxWriteBufferLimit = TransportUtils.DefaultDataBufferLimit
            };
            if (clientMutex != null)
            {
                _clientPipe.MutexApi = clientMutex;
            }
            SetMaxWriteBufferLimit(true, serverMaxWriteBufferLimit);
            SetMaxWriteBufferLimit(false, clientMaxWriteBufferLimit);
        }

        private void SetMaxWriteBufferLimit(bool fromServer, int maxWriteBufferLimit)
        {
            if (maxWriteBufferLimit <= 0)
            {
                return;
            }
            // set write buffer limit of the pipe for other participant.
            if (fromServer)
            {
                _clientPipe.MaxWriteBufferLimit = maxWriteBufferLimit;
            }
            else
            {
                _serverPipe.MaxWriteBufferLimit = maxWriteBufferLimit;
            }
        }

        public async Task<int> ProcessReadRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments.
            if (_connectionCancellationHandle.IsCancelled)
            {
                throw new ConnectionReleasedException();
            }
            var readReqProcessor = fromServer ? _serverPipe : _clientPipe;
            return await readReqProcessor.ReadBytes(data, offset, length);
        }

        public async Task ProcessWriteRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments.
            if (_connectionCancellationHandle.IsCancelled)
            {
                throw new ConnectionReleasedException();
            }
            // write to the pipe for other participant.
            var writeReqProcessor = fromServer ? _clientPipe : _serverPipe;
            // assumptions of write occuring in chunks or with an overall content length
            // remove need to ever call writeReqProcessor.WriteLastBytes()
            await writeReqProcessor.WriteBytes(data, offset, length);
        }

        public async Task Release()
        {
            if (!_connectionCancellationHandle.Cancel())
            {
                return;
            }
            var endOfReadError = new ConnectionReleasedException();
            await _serverPipe.EndRead(endOfReadError);
            await _clientPipe.EndRead(endOfReadError);
        }
    }
}
