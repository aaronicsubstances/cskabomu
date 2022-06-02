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

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request, 
            Action<Exception, IQuasiHttpResponse> cb)
        {
            var peer = Hub.Connections[remoteEndpoint];
            peer.Upstream.Application.ProcessRequest(request, cb);
        }

        public void SendMessage(object connection, byte[] data, int offset, int length,
            Action<Action<bool>> cancellationEnquirer, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var connection = new FakeTcpConnection(remoteEndpoint, this);
            cb.Invoke(null, connection);
        }

        public void ReleaseConnection(object connection)
        {
            var typedConnection = (FakeTcpConnection)connection;
            typedConnection.GetWriteStream(this).Dispose();
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var typedConnection = (FakeTcpConnection)connection;
            if (!typedConnection.ConnectionEstablished)
            {
                throw new Exception("cannot read from connection yet to be established");
            }
            var readStream = typedConnection.GetReadStream(this);
            readStream.Position = typedConnection.GetReadStreamPosition(this);
            int bytesRead = readStream.Read(data, offset, length);
            typedConnection.IncrementReadPosition(this, bytesRead);
            cb.Invoke(null, bytesRead);
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var typedConnection = (FakeTcpConnection)connection;
            FakeTcpTransport peer = null;
            if (!typedConnection.ConnectionEstablished)
            {
                peer = Hub.Connections[typedConnection._remoteEndpoint];
                typedConnection.EstablishConnection(peer);
            }
            var writeStream = typedConnection.GetWriteStream(this);
            writeStream.Position = writeStream.Length;
            writeStream.Write(data, offset, length);
            peer?.Upstream.OnReceive(typedConnection);
            cb.Invoke(null);
        }
    }
}
