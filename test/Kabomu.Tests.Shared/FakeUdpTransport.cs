﻿using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class FakeUdpTransport : IQuasiHttpTransport
    {
        public int MaxMessageOrChunkSize { get; set; }
        public bool IsByteOriented => false;
        public bool DirectSendRequestProcessingEnabled { get; set; }
        public FakeUdpTransportHub Hub { get; set; }
        public KabomuQuasiHttpClient Upstream { get; set; }
        public object LocalEndpoint { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request, 
            Action<Exception, IQuasiHttpResponse> cb)
        {
            var peer = Hub.Connections[remoteEndpoint];
            peer.Upstream.Application.ProcessRequest(request, cb);
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var connection = new FakeUdpConnection
            {
                RemoteEndpoint = remoteEndpoint
            };
            connection.Peer = new FakeUdpConnection
            {
                RemoteEndpoint = LocalEndpoint,
                Peer = connection
            };
            cb.Invoke(null, connection);
        }

        public void ReleaseConnection(object connection)
        {
            // nothing to do.
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(object connection, byte[] data, int offset, int length,
            Action<Action<bool>> cancellationEnquirer, Action<Exception> cb)
        {
            var typedConnection = (FakeUdpConnection)connection;
            var peer = Hub.Connections[typedConnection.RemoteEndpoint];
            cb.Invoke(null);
            peer.Upstream.OnReceiveMessage(typedConnection.Peer, data, offset, length);
        }
    }
}
