using Kabomu.Common.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

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

        public void ProcessReadRequest(IMutexApi mutex, object participant, 
            byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            // Rely on underlying backing body code's validation of arguments except for participant.
            if (participant == null)
            {
                throw new ArgumentException("null participating transport");
            }
            if (_releaseError != null)
            {
                cb.Invoke(_releaseError, 0);
                return;
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
            readProcessor.ReadBytes(mutex, data, offset, length, cb);
        }

        public void ProcessWriteRequest(IMutexApi mutex, object participant,
            byte[] data, int offset, int length, Action<Exception> cb)
        {
            // Rely on underlying backing body code's validation of arguments except for participant.
            if (participant == null)
            {
                throw new ArgumentException("null participating transport");
            }
            if (_releaseError != null)
            {
                cb.Invoke(_releaseError);
                return;
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
            writeProcessor.WriteBytes(mutex, data, offset, length, cb);
        }

        public void Release(IMutexApi mutex)
        {
            if (_releaseError != null)
            {
                return;
            }
            _releaseError = new Exception("connection reset");
            foreach (var processor in _readWriteRequestProcessors)
            {
                processor?.OnEndRead(mutex, _releaseError);
            }
        }
    }
}
