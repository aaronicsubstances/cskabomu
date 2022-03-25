using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Helpers
{
    public static class DateTimeUtils
    {
        public static long UnixTimeMillis
        {
            get
            {
                return ConvertToUnixTimeMillis(DateTime.Now);
            }
        }

        public static long ConvertToUnixTimeMillis(DateTime d)
        {
            return ((DateTimeOffset)d).ToUnixTimeMilliseconds();
        }
    }
}
