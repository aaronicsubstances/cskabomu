using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    internal class MemoryBasedTransportConnectionInternal
    {
        private readonly ICancellationHandle _connectionCancellationHandle = new DefaultCancellationHandle();
        private readonly PipeBackedBody _serverPipe;
        private readonly PipeBackedBody _clientPipe;

        public MemoryBasedTransportConnectionInternal(IMutexApi serverMutex, IMutexApi clientMutex)
        {
            _serverPipe = new PipeBackedBody();
            if (serverMutex != null)
            {
                _serverPipe.MutexApi = serverMutex;
            }
            _clientPipe = new PipeBackedBody();
            if (clientMutex != null)
            {
                _clientPipe.MutexApi = clientMutex;
            }
        }

        public async Task<int> ProcessReadRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments.
            if (_connectionCancellationHandle.IsCancelled)
            {
                throw new Exception("connection reset");
            }
            var readReqProcessor = fromServer ? _serverPipe : _clientPipe;
            return await readReqProcessor.ReadBytes(data, offset, length);
        }

        public async Task ProcessWriteRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments.
            if (_connectionCancellationHandle.IsCancelled)
            {
                throw new Exception("connection reset");
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
            await _serverPipe.EndRead();
            await _clientPipe.EndRead();
        }
    }
}
