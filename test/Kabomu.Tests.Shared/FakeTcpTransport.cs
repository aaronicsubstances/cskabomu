using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class FakeTcpTransport : IQuasiHttpTransport
    {
        public int MaxMessageOrChunkSize { get; set; }
        public bool IsByteOriented => true;
        public bool DirectSendRequestProcessingEnabled { get; set; }
        public FakeTcpTransportHub Hub { get; set; }
        public KabomuQuasiHttpClient Upstream { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequestMessage request, 
            Action<Exception, IQuasiHttpResponseMessage> cb)
        {
            var peer = Hub.Connections[remoteEndpoint];
            peer.Upstream.Application.ProcessRequest(request, cb);
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var connection = new FakeTcpConnection
            {
                RemoteEndpoint = remoteEndpoint
            };
            cb.Invoke(null, connection);
        }

        public void ReleaseConnection(object connection)
        {
            var typedConnection = (FakeTcpConnection)connection;
            typedConnection.DuplexStream.Dispose();
            if (typedConnection.ConnectionEstablished)
            {
                typedConnection.Peer.DuplexStream.Dispose();
            }
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var typedConnection = (FakeTcpConnection)connection;
            if (!typedConnection.ConnectionEstablished)
            {
                throw new Exception("cannot read from connection yet to be established");
            }
            int bytesRead = typedConnection.Peer.DuplexStream.Read(data, offset, length);
            cb.Invoke(null, bytesRead);
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var typedConnection = (FakeTcpConnection)connection;
            typedConnection.DuplexStream.Write(data, offset, length);
            cb.Invoke(null);
            if (!typedConnection.ConnectionEstablished)
            {
                var peer = Hub.Connections[typedConnection.RemoteEndpoint];
                typedConnection.MarkConnectionAsEstablished();
                peer.Upstream.OnReceive(typedConnection.Peer);
            }
        }

        public void SendMessage(object connection, byte[] data, int offset, int length, 
            Action<Action<bool>> cancellationEnquirer, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }
    }
}
