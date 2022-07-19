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
        public async Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            var path = (string)connectivityParams.RemoteEndpoint;
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            path = Path.Combine(Path.GetTempPath(), path);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
            var response = new DefaultConnectionAllocationResponse
            {
                Connection = socket
            };
            return response;
        }

        public Task ReleaseConnection(object connection)
        {
            return UnixDomainSocketServerTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return UnixDomainSocketServerTransport.ReadBytesInternal(connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return UnixDomainSocketServerTransport.WriteBytesInternal(connection, data, offset, length);
        }
    }
}

