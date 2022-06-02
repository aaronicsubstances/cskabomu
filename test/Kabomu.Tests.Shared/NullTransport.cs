using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public class NullTransport : IQuasiHttpTransport
    {
        private readonly object _expectedConnection;
        private readonly string[] _readChunks;
        private readonly int _maxWriteCount;
        private readonly StringBuilder _savedWrites;
        private int readIndex;
        private int _writeCount;

        public NullTransport(object expectedConnection,
            string[] readChunks, StringBuilder savedWrites, int maxWriteCount)
        {
            _expectedConnection = expectedConnection;
            _readChunks = readChunks;
            _savedWrites = savedWrites;
            _maxWriteCount = maxWriteCount;
        }

        public int MaxChunkSize { get; set; }

        public bool DirectSendRequestProcessingEnabled => false;

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

        public void ReadBytes(object connection, byte[] data, int offset, int length, 
            Action<Exception, int> cb)
        {
            Assert.Equal(_expectedConnection, connection);
            int nextBytesRead = 0;
            Exception e = null;
            if (readIndex < _readChunks.Length)
            {
                var nextReadChunk = Encoding.UTF8.GetBytes(_readChunks[readIndex++]);
                nextBytesRead = nextReadChunk.Length;
                Array.Copy(nextReadChunk, 0, data, offset, nextBytesRead);
            }
            else if (readIndex == _readChunks.Length)
            {
                readIndex++;
            }
            else
            {
                e = new Exception("END");
            }
            cb.Invoke(e, nextBytesRead);
        }

        public void WriteBytes(object connection, 
            byte[] data, int offset, int length,
            Action<Exception> cb)
        {
            Assert.Equal(_expectedConnection, connection);
            Assert.Equal(MaxChunkSize, data.Length);
            Exception e = null;
            if (_writeCount < _maxWriteCount)
            {
                _savedWrites.Append(Encoding.UTF8.GetString(data, offset, length));
                _writeCount++;
            }
            else
            {
                e = new Exception("END");
            }
            cb.Invoke(e);
        }
    }
}
