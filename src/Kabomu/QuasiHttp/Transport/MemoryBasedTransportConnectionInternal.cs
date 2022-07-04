using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    internal class MemoryBasedTransportConnectionInternal
    {
        private readonly WritableBackedBody[] _readWriteRequestProcessors;
        private bool _released;

        public MemoryBasedTransportConnectionInternal(MemoryBasedClientTransport initiatingParticipant, 
            object remoteEndpoint)
        {
            InitiatingParticipant = initiatingParticipant;
            RemoteEndpoint = remoteEndpoint;
            _readWriteRequestProcessors = new WritableBackedBody[2];
        }

        public MemoryBasedClientTransport InitiatingParticipant { get; }
        public object RemoteEndpoint { get; }

        public async Task<int> ProcessReadRequest(object participant, 
            byte[] data, int offset, int length)
        {
            // Rely on underlying backing body code's validation of arguments except for participant.
            if (participant == null)
            {
                throw new ArgumentException("null participating transport");
            }
            if (_released)
            {
                throw new Exception("connection reset");
            }
            // pick body at index for other participant for processing read request.
            int readReqProcessorIndex;
            if (participant == InitiatingParticipant)
            {
                readReqProcessorIndex = 1;
            }
            else
            {
                readReqProcessorIndex = 0;
            }
            if (_readWriteRequestProcessors[readReqProcessorIndex] == null)
            {
                _readWriteRequestProcessors[readReqProcessorIndex] = new WritableBackedBody(null);
            }
            var readProcessor = _readWriteRequestProcessors[readReqProcessorIndex];
            return await readProcessor.ReadBytes(data, offset, length);
        }

        public async Task ProcessWriteRequest(object participant,
            byte[] data, int offset, int length)
        {
            // Rely on underlying backing body code's validation of arguments except for participant.
            if (participant == null)
            {
                throw new ArgumentException("null participating transport");
            }
            if (_released)
            {
                throw new Exception("connection reset");
            }
            // pick body at index for participant for processing write request.
            int writeReqProcessorIndex;
            if (participant == InitiatingParticipant)
            {
                writeReqProcessorIndex = 0;
            }
            else
            {
                writeReqProcessorIndex = 1;
            }
            if (_readWriteRequestProcessors[writeReqProcessorIndex] == null)
            {
                _readWriteRequestProcessors[writeReqProcessorIndex] = new WritableBackedBody(null);
            }
            var writeProcessor = _readWriteRequestProcessors[writeReqProcessorIndex];
            // assumptions of write occuring in chunks or with an overall content length remove need to ever call
            // writeProcessor.WriteLastBytes()
            await writeProcessor.WriteBytes(data, offset, length);
        }

        public async Task Release()
        {
            if (_released)
            {
                return;
            }
            _released = true;
            var tasks = new List<Task>();
            foreach (var processor in _readWriteRequestProcessors)
            {
                if (processor != null)
                {
                    tasks.Add(processor.EndRead());
                }
            }
            await Task.WhenAll(tasks);
        }
    }
}
