using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class SocketBackedStream : ReadableStreamBase
    {
        private readonly Socket _socket;
        private readonly byte[] _tempBuffer;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="socket">the backing socket</param>
        /// <exception cref="ArgumentNullException">The <paramref name="socket"/> argument is null.</exception>
        public SocketBackedStream(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
            _socket = socket;
            _tempBuffer = new byte[1];
        }

        public override int ReadByte()
        {
            int bytesRead = Read(_tempBuffer);
            if (bytesRead > 0)
            {
                return _tempBuffer[0];
            }
            else
            {
                return -1;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return _socket.ReceiveAsync(buffer, SocketFlags.None,
                cancellationToken);
        }
    }
}
