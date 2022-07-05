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
        private readonly WritableBackedBody _serverReadReqProcessor;
        private readonly WritableBackedBody _clientReadReqProcessor;
        private bool _released;

        public MemoryBasedTransportConnectionInternal(IMutexApi serverMutex, IMutexApi clientMutex)
        {
            _serverReadReqProcessor = new WritableBackedBody(null)
            {
                MutexApi = serverMutex
            };
            _clientReadReqProcessor = new WritableBackedBody(null)
            {
                MutexApi = clientMutex
            };
        }

        public async Task<int> ProcessReadRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments as well ensure proper mutex.
            if (_released)
            {
                throw new Exception("connection reset");
            }
            var readReqProcessor = fromServer ? _serverReadReqProcessor : _clientReadReqProcessor;
            return await readReqProcessor.ReadBytes(data, offset, length);
        }

        public async Task ProcessWriteRequest(bool fromServer, byte[] data, int offset, int length)
        {
            // Rely on caller to supply valid arguments as well ensure proper mutex.
            if (_released)
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
            // Rely on caller to ensure proper mutex.
            if (_released)
            {
                return;
            }
            _released = true;
            await _serverReadReqProcessor.EndRead();
            await _clientReadReqProcessor.EndRead();
        }
    }
}
