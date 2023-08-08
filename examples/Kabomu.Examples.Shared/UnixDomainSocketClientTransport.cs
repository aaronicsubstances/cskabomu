using Kabomu.QuasiHttp.Transport;
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
        public async Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpSendOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            path = Path.Combine(Path.GetTempPath(), path);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
            var response = new DefaultConnectionAllocationResponse
            {
                Connection = new SocketWrapper(socket)
            };
            return response;
        }

        public object GetWriter(object connection)
        {
            return UnixDomainSocketServerTransport.GetWriterInternal(connection);
        }

        public object GetReader(object connection)
        {
            return UnixDomainSocketServerTransport.GetReaderInternal(connection);
        }

        public Task ReleaseConnection(object connection)
        {
            return UnixDomainSocketServerTransport.ReleaseConnectionInternal(connection);
        }
    }
}

