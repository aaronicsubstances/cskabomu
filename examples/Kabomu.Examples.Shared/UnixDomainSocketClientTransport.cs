﻿using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class UnixDomainSocketClientTransport : IQuasiHttpClientTransport
    {
        public async Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            path = Path.Combine(Path.GetTempPath(), path);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
            return new SocketConnection(socket, true,
                sendOptions, DefaultSendOptions);
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public Task ReleaseConnection(IQuasiHttpConnection connection)
        {
            return ((SocketConnection)connection).Release();
        }

        public Task Write(IQuasiHttpConnection connection, bool isResponse,
            IEncodedQuasiHttpEntity entity)
        {
            return ((SocketConnection)connection).Write(isResponse, entity);
        }

        public Task<IEncodedQuasiHttpEntity> Read(
            IQuasiHttpConnection connection,
            bool isResponse)
        {
            return ((SocketConnection)connection).Read(
                isResponse);
        }
    }
}
