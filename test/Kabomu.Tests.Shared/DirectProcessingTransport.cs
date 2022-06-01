using Kabomu.Common;
using System;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class DirectProcessingTransport : IQuasiHttpTransport
    {
        private readonly object _expectedRemoteEndpoint;
        private Action<IQuasiHttpRequest, Action<Exception, IQuasiHttpResponse>> _processingCb;

        public DirectProcessingTransport(object expectedRemoteEndpoint, 
            Action<IQuasiHttpRequest, Action<Exception, IQuasiHttpResponse>> cb)
        {
            _expectedRemoteEndpoint = expectedRemoteEndpoint;
            _processingCb = cb;
        }

        public int MaxMessageOrChunkSize => throw new NotImplementedException();

        public bool IsByteOriented { get; set; }

        public bool DirectSendRequestProcessingEnabled => true;

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
        {
            Assert.Equal(_expectedRemoteEndpoint, remoteEndpoint);
            _processingCb.Invoke(request, cb);
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            throw new NotImplementedException();
        }

        public void ReleaseConnection(object connection)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(object connection, byte[] data, int offset, int length, 
            Action<Action<bool>> cancellationEnquirer, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }
    }
}