using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class DemoCustomReaderWritable : ICustomReader, ICustomWritable
    {
        private readonly Random _randGen = new Random();
        private readonly byte[] _srcData;
        private bool _disposed;
        private int _bytesRead;

        public bool TurnOffRandomization { get; set; }

        public DemoCustomReaderWritable() :
            this(new byte[0])
        {
        }

        public DemoCustomReaderWritable(byte[] srcData)
        {
            _srcData = srcData;
        }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("reader");
            }
            var bytesToCopy = Math.Min(_srcData.Length - _bytesRead, length);
            if (bytesToCopy > 0)
            {
                if (!TurnOffRandomization)
                {
                    // copy just a random quantity out of the remaining bytes
                    bytesToCopy = _randGen.Next(bytesToCopy) + 1;
                }
                Array.Copy(_srcData, _bytesRead, data, offset, bytesToCopy);
                _bytesRead += bytesToCopy;
            }
            return Task.FromResult(bytesToCopy);
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
