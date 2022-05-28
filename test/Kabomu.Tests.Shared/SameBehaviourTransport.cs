using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class SameBehaviourTransport : IQuasiHttpTransport
    {
        private readonly Exception _readError;
        private readonly int _readLen;
        private readonly Exception _writeError;

        public SameBehaviourTransport(Exception readError, int readLen, Exception writeError)
        {
            _readError = readError;
            _readLen = readLen;
            _writeError = writeError;
        }

        public int MaxMessageOrChunkSize => throw new NotImplementedException();

        public bool IsByteOriented => throw new NotImplementedException();

        public bool DirectSendRequestProcessingEnabled => throw new NotImplementedException();

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            throw new NotImplementedException();
        }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequestMessage request, Action<Exception, IQuasiHttpResponseMessage> cb)
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            cb.Invoke(_readError, _readLen);
        }

        public void ReleaseConnection(object connection)
        {
            throw new NotImplementedException();
        }

        public void WriteBytesOrSendMessage(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            cb.Invoke(_writeError);
        }
    }
}
