using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpTransport : IQuasiHttpTransport
    {
        public int MaxChunkSize { get; set; }

        public bool DirectSendRequestProcessingEnabled { get; set; }

        public Action<object, IQuasiHttpRequest, Action<Exception, IQuasiHttpResponse>> ProcessSendRequestCallback { get; set; }

        public Action<object, Action<Exception, object>> AllocateConnectionCallback { get; set; }

        public Action<object> ReleaseConnectionCallback { get; set; }

        public Action<object, byte[], int, int, Action<Exception, int>> ReadBytesCallback { get; set; }

        public Action<object, byte[], int, int, Action<Exception>> WriteBytesCallback { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
        {
            ProcessSendRequestCallback?.Invoke(remoteEndpoint, request, cb);
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            AllocateConnectionCallback?.Invoke(remoteEndpoint, cb);
        }

        public void OnReleaseConnection(object connection)
        {
            ReleaseConnectionCallback?.Invoke(connection);
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            ReadBytesCallback?.Invoke(connection, data, offset, length, cb);
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            WriteBytesCallback?.Invoke(connection, data, offset, length, cb);
        }
    }
}
