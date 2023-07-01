using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class DemoSimpleCustomWriter : ICustomWriter
    {
        private bool _disposed;

        public DemoSimpleCustomWriter()
        {
            Buffer = new StringBuilder();
        }

        public string ChunkMarker { get; set; }

        public StringBuilder Buffer { get; }

        public Task WriteBytes(byte[] data, int offset, int length)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("writer");
            }
            Buffer.Append(Encoding.UTF8.GetString(data, offset, length));
            Buffer.Append(ChunkMarker ?? "");
            return Task.CompletedTask;
        }

        public Task CustomDispose()
        {
            _disposed = true;
            return Task.CompletedTask;
        }
    }
}
