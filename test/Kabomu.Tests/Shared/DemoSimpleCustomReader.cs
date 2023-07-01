using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class DemoSimpleCustomReader : ICustomReader
    {
        private readonly byte[] _srcData;
        private bool _disposed;
        private int _bytesRead;

        public DemoSimpleCustomReader() :
            this(new byte[0])
        {
        }

        public DemoSimpleCustomReader(byte[] srcData)
        {
            _srcData = srcData;
        }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("reader");
            }
            int bytesToCopy = 0;
            if (length > 0 && _bytesRead < _srcData.Length)
            {
                data[offset] = _srcData[_bytesRead++];
                bytesToCopy = 1;
            }
            return Task.FromResult(bytesToCopy);
        }

        public Task CustomDispose()
        {
            _disposed = true;
            return Task.CompletedTask;
        }
    }
}
