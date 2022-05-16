using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public static class MiscUtils
    {
        public static int[] CalculateRetryBackoffRange(int timePeriod, int retryCount)
        {
            int minBackoff = timePeriod / (retryCount + 1);
            int maxBackoff = timePeriod / retryCount;
            return new int[] { minBackoff, maxBackoff };
        }
    }
}
