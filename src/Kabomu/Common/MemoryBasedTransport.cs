using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class MemoryBasedTransport : IQuasiHttpTransport
    {
        public object LocalEndpoint { get; set; }
        public MemoryBasedTransportHub Hub { get; set; }

        public int MaxChunkSize { get; set; }

        public bool DirectSendRequestProcessingEnabled { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
        {
            var remoteClient = Hub.Connections[remoteEndpoint];
            remoteClient.Application.ProcessRequest(request, cb);
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var connection = new MemoryBasedTransportConnection(remoteEndpoint);
            cb.Invoke(null, connection);
        }

        public void ReleaseConnection(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnection)connection;
            typedConnection.Release();
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var typedConnection = (MemoryBasedTransportConnection)connection;
            typedConnection.ProcessReadRequest(LocalEndpoint, data, offset, length, cb);
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var typedConnection = (MemoryBasedTransportConnection)connection;
            typedConnection.ProcessWriteRequest(LocalEndpoint, data, offset, length, cb);
            if (!typedConnection.IsConnectionEstablished)
            {
                var remoteClient = Hub.Connections[typedConnection.RemoteEndpoint];
                typedConnection.MarkConnectionAsEstablished();
                remoteClient.OnReceive(connection);
            }
        }
    }
}
