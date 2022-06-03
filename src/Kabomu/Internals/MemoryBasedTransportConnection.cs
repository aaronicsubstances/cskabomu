using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class MemoryBasedTransportConnection
    {
        private readonly Dictionary<object, ReadWriteRequest> _readRequests;
        private readonly Dictionary<object, List<ReadWriteRequest>> _writeRequests;
        private Exception _releaseError;

        public MemoryBasedTransportConnection(object remoteEndpoint)
        {
            // Rely on Transport code's validation of arguments.
            RemoteEndpoint = remoteEndpoint;
            _readRequests = new Dictionary<object, ReadWriteRequest>();
            _writeRequests = new Dictionary<object, List<ReadWriteRequest>>();
        }

        public object RemoteEndpoint { get; private set; }

        public bool IsConnectionEstablished => RemoteEndpoint != null;

        public void MarkConnectionAsEstablished()
        {
            RemoteEndpoint = null;
        }

        public void ProcessReadRequest(object localEndpoint, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            // Rely on Transport code's validation of arguments except for localEndpoint.
            if (localEndpoint == null)
            {
                throw new ArgumentException("null local endpoint");
            }
            if (_releaseError != null)
            {
                cb.Invoke(_releaseError, 0);
                return;
            }
            var readRequest = new ReadWriteRequest
            {
                Data = data,
                Offset = offset,
                Length = length,
                ReadCallback = cb
            };
            _readRequests.Add(localEndpoint, readRequest);

            object remoteEndpoint = null;
            foreach (var key in _writeRequests.Keys)
            {
                if (key != localEndpoint)
                {
                    remoteEndpoint = key;
                    break;
                }
            }
            if (remoteEndpoint != null && _writeRequests[remoteEndpoint].Count > 0)
            {
                var earliestPendingWriteRequest = _writeRequests[remoteEndpoint][0];
                MatchPendingWriteAndRead(earliestPendingWriteRequest, readRequest);
            }
        }

        public void ProcessWriteRequest(object localEndpoint, byte[] data, int offset, int length, Action<Exception> cb)
        {
            // Rely on Transport code's validation of arguments except for localEndpoint.
            if (localEndpoint == null)
            {
                throw new ArgumentException("null local endpoint");
            }
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
            if (!_writeRequests.ContainsKey(localEndpoint))
            {
                _writeRequests.Add(localEndpoint, new List<ReadWriteRequest>());
            }
            _writeRequests[localEndpoint].Add(writeRequest);

            ReadWriteRequest earliestPendingReadRequest = null;
            foreach (var entry in _readRequests.Values)
            {
                if (entry.LocalEndpoint != localEndpoint)
                {
                    earliestPendingReadRequest = entry;
                    break;
                }
            }
            if (earliestPendingReadRequest == null)
            {
                return;
            }

            var earliestPendingWriteRequest = _writeRequests[localEndpoint][0];
            MatchPendingWriteAndRead(earliestPendingWriteRequest, earliestPendingReadRequest);
        }

        private void MatchPendingWriteAndRead(ReadWriteRequest pendingWrite, ReadWriteRequest pendingRead)
        {
            var bytesToReturn = Math.Min(pendingWrite.Length, pendingRead.Length);
            Array.Copy(pendingWrite.Data, pendingWrite.Offset,
                pendingRead.Data, pendingRead.Offset, bytesToReturn);
            _readRequests.Remove(pendingRead.LocalEndpoint);
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
            _releaseError = new Exception("connection reset");
            foreach (var entry in _readRequests.Values)
            {
                entry.ReadCallback.Invoke(_releaseError, 0);
            }
            foreach (var entry in _writeRequests.Values)
            {
                foreach (var value in entry)
                {
                    value.WriteCallback.Invoke(_releaseError);
                }
            }
            _readRequests.Clear();
            _writeRequests.Clear();
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
