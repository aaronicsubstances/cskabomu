using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    /// <summary>
    /// Copied from
    /// https://docs.oracle.com/javase/8/docs/api/java/util/Random.html#nextInt--
    /// </summary>
    internal class STMessageIdGenerator : IMessageIdGenerator
    {
        private long _seed;

        public STMessageIdGenerator(long seed)
        {
            _seed = (seed ^ 0x5DEECE66DL) & ((1L << 48) - 1);
        }

        public long NextId()
        {
            _seed = (_seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);
            return _seed;
        }
    }
}
