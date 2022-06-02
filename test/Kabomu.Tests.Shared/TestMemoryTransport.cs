using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public class TestMemoryTransport : IQuasiHttpTransport
    {
        private readonly object _expectedRemoteEndpoint;
        private readonly object _expectedConnection;
        private readonly MemoryStream _inputStream;
        private readonly MemoryStream _outputStream;

        public TestMemoryTransport(object expectedRemoteEndpoint, object expectedConnection,
            MemoryStream inputStream, MemoryStream outputStream, int maxChunkSize)
        {
            _expectedRemoteEndpoint = expectedRemoteEndpoint;
            _expectedConnection = expectedConnection;
            _inputStream = inputStream;
            _outputStream = outputStream;
            MaxChunkSize = maxChunkSize;
        }

        public int MaxChunkSize { get; }

        public bool DirectSendRequestProcessingEnabled => false;

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request,
            Action<Exception, IQuasiHttpResponse> cb)
        {
            throw new NotImplementedException();
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            Assert.Equal(_expectedRemoteEndpoint, remoteEndpoint);
            cb.Invoke(null, _expectedConnection);
            // test handling of multiple callback invocations.
            cb.Invoke(null, _expectedConnection);
        }

        public void ReleaseConnection(object connection)
        {
            Assert.Equal(_expectedConnection, connection);
            // nothing to do again.
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
