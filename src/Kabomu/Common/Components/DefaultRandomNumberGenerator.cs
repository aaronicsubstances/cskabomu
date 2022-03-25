using Kabomu.Common.Abstractions;
using Kabomu.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class DefaultRandomNumberGenerator : IRandomNumberGenerator
    {
        public long NextId()
        {
            int lo = MathUtils.GetRandomInt32(int.MaxValue);
            long hi = MathUtils.GetRandomInt32(int.MaxValue);
            return (hi << 32) + lo;
        }
    }
}
