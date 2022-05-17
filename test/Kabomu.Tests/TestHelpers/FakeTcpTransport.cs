using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Tests.TestHelpers
{
    public class FakeTcpTransport : IQuasiHttpTransport
    {
        public int MaxMessageSize => throw new NotImplementedException();

        public bool IsByteOriented => true;

        public bool DirectSendRequestProcessingEnabled => false;

        public FakeTcpTransportHub Hub { get; set; }
        public IQuasiHttpClient Upstream { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, QuasiHttpRequestMessage request, Action<Exception, QuasiHttpResponseMessage> cb)
        {
            throw new NotImplementedException();
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var connection = new FakeTcpConnection
            {
                RemoteEndpoint = remoteEndpoint,
                DuplexStream = new MemoryStream()
            };
            cb.Invoke(null, connection);
        }

        public void ReleaseConnection(object connection)
        {
            var typedConnection = (FakeTcpConnection)connection;
            typedConnection.DuplexStream.Dispose();
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var typedConnection = (FakeTcpConnection)connection;
            int bytesRead = typedConnection.DuplexStream.Read(data, offset, length);
            cb.Invoke(null, bytesRead);
        }

        public void WriteBytesOrSendMessage(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var typedConnection = (FakeTcpConnection)connection;
            typedConnection.DuplexStream.Write(data, offset, length);
            cb.Invoke(null);
            if (!typedConnection.ConnectionEstablished)
            {
                var peer = Hub.Connections[typedConnection.RemoteEndpoint];
                typedConnection.RemoteEndpoint = null;
                peer.Upstream.OnReceiveConnection(typedConnection);
            }
        }
    }
}
