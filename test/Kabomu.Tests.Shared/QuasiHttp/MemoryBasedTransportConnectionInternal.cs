using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    internal class MemoryBasedTransportConnectionInternal
    {
        private readonly bool _fireAndForget;
        private readonly MemoryPipeCustomReaderWriter _serverPipe;
        private readonly MemoryPipeCustomReaderWriter _clientPipe;

        public MemoryBasedTransportConnectionInternal() :
            this(null)
        {
        }

        public MemoryBasedTransportConnectionInternal(bool? fireAndForget)
        {
            _serverPipe = new MemoryPipeCustomReaderWriter();
            _clientPipe = new MemoryPipeCustomReaderWriter();
            _fireAndForget = fireAndForget ?? false;
        }

        public async Task<int> ProcessReadRequest(bool fromServer, byte[] data, int offset, int length)
        {
            if (!fromServer && _fireAndForget)
            {
                return 0;
            }
            // Rely on caller to supply valid arguments.
            var readReqProcessor = fromServer ? _serverPipe : _clientPipe;
            return await readReqProcessor.ReadBytes(data, offset, length);
        }

        public async Task ProcessWriteRequest(bool fromServer, byte[] data, int offset, int length)
        {
            if (fromServer && _fireAndForget)
            {
                //return;
            }
            // write to the pipe for other participant.
            var writeReqProcessor = fromServer ? _clientPipe : _serverPipe;
            await writeReqProcessor.WriteBytes(data, offset, length);
        }

        public async Task Release()
        {
            await _serverPipe.CustomDispose();
            await _clientPipe.CustomDispose();
        }
    }
}
