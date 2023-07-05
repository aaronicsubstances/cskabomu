using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class DemoSimpleCustomWritable : ICustomWritable
    {
        private readonly byte[] _srcData;
        private bool _disposed;

        public DemoSimpleCustomWritable() :
            this(null)
        {
        }

        public DemoSimpleCustomWritable(byte[] srcData)
        {
            _srcData = srcData ?? new byte[0];
        }

        public Task CustomDispose()
        {
            _disposed = true;
            return Task.CompletedTask;
        }

        public Task WriteBytesTo(ICustomWriter writer)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("writable");
            }
            return writer.WriteBytes(_srcData, 0, _srcData.Length);
        }
    }
}
