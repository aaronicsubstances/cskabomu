using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.Common
{
    public class RandomizedReadSizeBufferReader : ICustomReader
    {
        private readonly Random _randGen = new Random();
        private readonly MemoryStream _stream;

        public RandomizedReadSizeBufferReader(byte[] srcData)
        {
            _stream = new MemoryStream(srcData);
            _stream.Position = 0; // rewind for reading
        }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            var bytesToCopy = (int)Math.Min(_stream.Length - _stream.Position, length);
            if (bytesToCopy > 0)
            {
                // copy just a random quantity out of the remaining bytes
                bytesToCopy = _randGen.Next(bytesToCopy) + 1;
            }
            return _stream.ReadAsync(data, offset, bytesToCopy);
        }
    }
}
