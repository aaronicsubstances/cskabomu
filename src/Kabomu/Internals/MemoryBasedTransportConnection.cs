using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class MemoryBasedTransportConnection
    {
        private object _remoteEndpoint;
        private readonly Dictionary<object, ReadWriteRequest> _readRequests;
        private readonly Dictionary<object, ReadWriteRequest> _writeRequests;
        private Exception _releaseError;

        public MemoryBasedTransportConnection(object remoteEndpoint)
        {
            RemoteEndpoint = remoteEndpoint ?? new ArgumentException("null remoteEndpoint");
            _readRequests = new Dictionary<object, ReadWriteRequest>();
            _writeRequests = new Dictionary<object, ReadWriteRequest>();
        }

        public object RemoteEndpoint { get; private set; }

        public bool IsConnectionEstablished => RemoteEndpoint != null;

        public void MarkConnectionAsEstablished()
        {
            RemoteEndpoint = null;
        }

        public void ProcessReadRequest(object localEndpoint, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            if (_releaseError != null)
            {
                cb.Invoke(_releaseError, 0);
                return;
            }
            var readRequest = new ReadWriteRequest
            {
                LocalEndpoint = localEndpoint,
                Data = data,
                Offset = offset,
                Length = length,
                ReadCallback = cb
            };
            _readRequests.Add(localEndpoint, readRequest);

            ReadWriteRequest awaitingWriteRequest = null;
            foreach (var entry in _writeRequests.Values)
            {
                if (entry.LocalEndpoint != localEndpoint)
                {
                    awaitingWriteRequest = entry;
                    break;
                }
            }
            if (awaitingWriteRequest == null)
            {
                return;
            }

            MatchPendingWriteAndRead(awaitingWriteRequest, readRequest);
        }

        public void ProcessWriteRequest(object localEndpoint, byte[] data, int offset, int length, Action<Exception> cb)
        {
            if (_releaseError != null)
            {
                cb.Invoke(_releaseError);
                return;
            }
            var writeRequest = new ReadWriteRequest
            {
                LocalEndpoint = localEndpoint,
                Data = data,
                Offset = offset,
                Length = length,
                WriteCallback = cb
            };
            _writeRequests.Add(localEndpoint, writeRequest);

            ReadWriteRequest awaitingReadRequest = null;
            foreach (var entry in _readRequests.Values)
            {
                if (entry.LocalEndpoint != localEndpoint)
                {
                    awaitingReadRequest = entry;
                    break;
                }
            }
            if (awaitingReadRequest == null)
            {
                return;
            }

            MatchPendingWriteAndRead(writeRequest, awaitingReadRequest);
        }

        private void MatchPendingWriteAndRead(ReadWriteRequest pendingWrite, ReadWriteRequest pendingRead)
        {
            _readRequests.Remove(pendingRead.LocalEndpoint);
            var bytesToReturn = Math.Min(pendingWrite.Length, pendingRead.Length);
            if (bytesToReturn < pendingWrite.Length)
            {
                pendingWrite.Offset += bytesToReturn;
                pendingWrite.Length -= bytesToReturn;
            }
            else
            {
                _writeRequests.Remove(pendingWrite.LocalEndpoint);
            }
            pendingRead.ReadCallback.Invoke(null, bytesToReturn);
            pendingWrite.WriteCallback.Invoke(null);
        }

        public void Release()
        {
            if (_releaseError != null)
            {
                return;
            }
            _releaseError = new Exception("released");
            foreach (var entry in _writeRequests.Values)
            {
                entry.WriteCallback.Invoke(_releaseError);
            }
            foreach (var entry in _readRequests.Values)
            {
                entry.ReadCallback.Invoke(_releaseError, 0);
            }
            _writeRequests.Clear();
            _readRequests.Clear();
        }

        private class ReadWriteRequest
        {
            public object LocalEndpoint { get; set; }
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public Action<Exception> WriteCallback { get; set; }
            public Action<Exception, int> ReadCallback { get; set; }
        }
    }
}
