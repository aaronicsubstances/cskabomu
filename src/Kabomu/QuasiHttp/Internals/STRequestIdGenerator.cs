using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    /// <summary>
    /// Based on MINSTD PRNG
    /// </summary>
    internal class STRequestIdGenerator : IRequestIdGenerator
    {
        private int _seed;

        public STRequestIdGenerator(long seed)
        {
            _seed = (int)(seed % int.MaxValue);
            if (_seed < 0)
            {
                // MINSTD doesn't generate negative numbers.
                _seed = (_seed + int.MaxValue) % int.MaxValue;
            }
            else if (_seed == 0)
            {
                // MINSTD doesn't generate 0.
                _seed = 1;
            }
        }

        public int NextId()
        {
            _seed = (int)((_seed * 16807L) % int.MaxValue);
            return _seed;
        }
    }
}
