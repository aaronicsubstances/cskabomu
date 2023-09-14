using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    internal class RandomizedReadInputStream : ReadableStreamBaseInternal
    {
        private readonly Stream _backingStream;
        private readonly bool _failOnZeroByteReads;

        public RandomizedReadInputStream(string srcData):
            this(MiscUtilsInternal.StringToBytes(srcData))
        { }

        public RandomizedReadInputStream(byte[] srcData):
            this(new MemoryStream(srcData))
        { }

        public RandomizedReadInputStream(Stream backingStream,
            bool failOnZeroByteReads = true)
        {
            _backingStream = backingStream;
            _failOnZeroByteReads = failOnZeroByteReads;
        }

        public override int ReadByte()
        {
            return _backingStream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0 && _failOnZeroByteReads)
            {
                throw new NotSupportedException("zero byte reads not allowed");
            }
            if (count > 1)
            {
                count = Random.Shared.Next(count) + 1;
            }
            return _backingStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset,
            int count, CancellationToken cancellationToken)
        {
            if (count == 0 && _failOnZeroByteReads)
            {
                throw new NotSupportedException("zero byte reads not allowed");
            }
            if (count > 1)
            {
                count = Random.Shared.Next(count) + 1;
            }
            return _backingStream.ReadAsync(buffer, offset, count,
                cancellationToken);
        }
    }
}
