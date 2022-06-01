using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public class MemoryStreamTransport : IQuasiHttpTransport
    {
        private readonly object _expectedConnection;
        private readonly MemoryStream _inputStream;
        private readonly MemoryStream _outputStream;

        public MemoryStreamTransport(object expectedConnection, MemoryStream inputStream,
            MemoryStream outputStream, int maxChunkSize)
        {
            _expectedConnection = expectedConnection;
            _inputStream = inputStream;
            _outputStream = outputStream;
            MaxMessageOrChunkSize = maxChunkSize;
        }

        public int MaxMessageOrChunkSize { get; }

        public bool IsByteOriented => true;

        public bool DirectSendRequestProcessingEnabled => throw new NotImplementedException();

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request,
            Action<Exception, IQuasiHttpResponse> cb)
        {
            throw new NotImplementedException();
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
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

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            Assert.Equal(_expectedConnection, connection);
            var bytesRead = _inputStream.Read(data, offset, length);
            cb.Invoke(null, bytesRead);
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            Assert.Equal(_expectedConnection, connection);
            _outputStream.Write(data, offset, length);
            cb.Invoke(null);
            // test handling of repeated callback invocations
            cb.Invoke(null);
        }
    }
}
