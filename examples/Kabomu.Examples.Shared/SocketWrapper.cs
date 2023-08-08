using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class SocketWrapper
    {
        public SocketWrapper(Socket socket)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            Reader = new LambdaBasedCustomReader
            {
                ReadFunc = async (data, offset, length) =>
                {
                    return await socket.ReceiveAsync(new Memory<byte>(data, offset, length),
                        SocketFlags.None);
                }
            };
            Writer = new LambdaBasedCustomWriter
            {
                WriteFunc = async (data, offset, length) =>
                {
                    int totalBytesSent = 0;
                    while (totalBytesSent < length)
                    {
                        int bytesSent = await socket.SendAsync(
                            new ReadOnlyMemory<byte>(data, offset + totalBytesSent, length - totalBytesSent), SocketFlags.None);
                        totalBytesSent += bytesSent;
                    }
                }
            };
        }

        public Socket Socket { get; }
        public ICustomReader Reader { get; }
        public ICustomWriter Writer { get; }
    }
}
