using Kabomu.Common.Bodies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Transports
{
    internal class MemoryBasedTransportConnectionInternal
    {
        private readonly WritableBackedBody[] _readWriteRequestProcessors;
        private readonly object _initiatingParticipant;
        private Exception _releaseError;

        public MemoryBasedTransportConnectionInternal(object initiatingParticipant)
        {
            _initiatingParticipant = initiatingParticipant;
            _readWriteRequestProcessors = new WritableBackedBody[2];
        }

        public async Task<int> ProcessReadRequestAsync(IEventLoopApi eventLoop, object participant, 
            byte[] data, int offset, int length)
        {
            // Rely on underlying backing body code's validation of arguments except for participant.
            if (participant == null)
            {
                throw new ArgumentException("null participating transport");
            }
            if (_releaseError != null)
            {
                throw _releaseError;
            }
            // pick body at index for other participant for processing read request.
            int readReqProcessorIndex;
            if (participant == _initiatingParticipant)
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
            return await readProcessor.ReadBytesAsync(eventLoop, data, offset, length);
        }

        public async Task ProcessWriteRequestAsync(IEventLoopApi eventLoop, object participant,
            byte[] data, int offset, int length)
        {
            // Rely on underlying backing body code's validation of arguments except for participant.
            if (participant == null)
            {
                throw new ArgumentException("null participating transport");
            }
            if (_releaseError != null)
            {
                throw _releaseError;
            }
            // pick body at index for participant for processing write request.
            int writeReqProcessorIndex;
            if (participant == _initiatingParticipant)
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
            await writeProcessor.WriteBytesAsync(eventLoop, data, offset, length);
        }

        public async Task ReleaseAsync(IEventLoopApi eventLoop)
        {
            if (_releaseError != null)
            {
                return;
            }
            _releaseError = new Exception("connection reset");
            foreach (var processor in _readWriteRequestProcessors)
            {
                if (processor != null)
                {
                    await processor.EndReadAsync(eventLoop, _releaseError);
                }
            }
        }
    }
}
