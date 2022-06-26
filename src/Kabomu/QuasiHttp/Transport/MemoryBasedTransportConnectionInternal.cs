﻿using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    internal class MemoryBasedTransportConnectionInternal
    {
        private readonly WritableBackedBody[] _readWriteRequestProcessors;
        private Exception _releaseError;

        public MemoryBasedTransportConnectionInternal(object initiatingParticipant, 
            object initiatingParticipantEndpoint, object remoteParticipantEndpoint)
        {
            InitiatingParticipant = initiatingParticipant;
            InitiatingParticipantEndpoint = initiatingParticipantEndpoint;
            RemoteParticipantEndpoint = remoteParticipantEndpoint;
            _readWriteRequestProcessors = new WritableBackedBody[2];
        }
        public object InitiatingParticipant { get; }
        public object InitiatingParticipantEndpoint { get; }
        public object RemoteParticipantEndpoint { get; }

        public async Task<int> ProcessReadRequest(object participant, 
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
            if (_releaseError != null)
            {
                throw _releaseError;
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
            if (_releaseError != null)
            {
                return;
            }
            _releaseError = new Exception("connection reset");
            var tasks = new List<Task>();
            foreach (var processor in _readWriteRequestProcessors)
            {
                if (processor != null)
                {
                    tasks.Add(processor.EndRead(_releaseError));
                }
            }
            await Task.WhenAll(tasks);
        }
    }
}
