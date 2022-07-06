using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    internal class MemoryBasedTransportConnectionInternal
    {
        private readonly CancellationTokenSource _connectionCancellationHandle = new CancellationTokenSource();
        private readonly WritableBackedBody _serverReadReqProcessor;
        private readonly WritableBackedBody _clientReadReqProcessor;

        public MemoryBasedTransportConnectionInternal(IMutexApi serverMutex, IMutexApi clientMutex)
        {
            _serverReadReqProcessor = new WritableBackedBody(null);
            if (serverMutex != null)
            {
                _serverReadReqProcessor.MutexApi = serverMutex;
            }
            _clientReadReqProcessor = new WritableBackedBody(null);
            if (clientMutex != null)
            {
                _clientReadReqProcessor.MutexApi = clientMutex;
            }
        }

        public async Task<int> ProcessReadRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments.
            if (_connectionCancellationHandle.IsCancellationRequested)
            {
                throw new Exception("connection reset");
            }
            var readReqProcessor = fromServer ? _serverReadReqProcessor : _clientReadReqProcessor;
            return await readReqProcessor.ReadBytes(data, offset, length);
        }

        public async Task ProcessWriteRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments.
            if (_connectionCancellationHandle.IsCancellationRequested)
            {
                throw new Exception("connection reset");
            }
            // pick read req processor for other participant for processing writes.
            var writeReqProcessor = !fromServer ? _serverReadReqProcessor : _clientReadReqProcessor;
            // assumptions of write occuring in chunks or with an overall content length
            // remove need to ever call writeReqProcessor.WriteLastBytes()
            await writeReqProcessor.WriteBytes(data, offset, length);
        }

        public async Task Release()
        {
            if (_connectionCancellationHandle.IsCancellationRequested)
            {
                return;
            }
            _connectionCancellationHandle.Cancel();
            await _serverReadReqProcessor.EndRead();
            await _clientReadReqProcessor.EndRead();
        }
    }
}
