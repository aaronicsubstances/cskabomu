using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Helpers
{
    public static class MathUtils
    {
        private static readonly Random RandNumGen = new Random();

        public static int GetRandomInt32(int max)
        {
            return RandNumGen.Next(max);
        }

        public static bool GetRandomBoolean()
        {
            return GetRandomInt32(2) == 0;
        }
    }
}
