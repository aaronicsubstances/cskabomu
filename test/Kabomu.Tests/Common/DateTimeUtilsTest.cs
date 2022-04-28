using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class DateTimeUtilsTest
    {
        [Theory]
        [MemberData(nameof(CreateConvertToUnixTimeMillisData))]
        public void TestConvertToUnixTimeMillis(DateTime d, long expected)
        {
            long actual = DateTimeUtils.ConvertToUnixTimeMillis(d);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateConvertToUnixTimeMillisData()
        {
            return new List<object[]>
            {
                new object[]{ new DateTime(1957, 10, 4, 0, 0, 0, 201, DateTimeKind.Utc), -386_380_799_799L },
                new object[]{ new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0L },
                new object[]{ new DateTime(1973, 10, 17, 18, 36, 57, DateTimeKind.Utc), 119_731_017_000L },
                new object[]{ new DateTime(2001, 9, 9, 1, 46, 40, DateTimeKind.Utc), 1_000_000_000_000L },
                new object[]{ new DateTime(2004, 9, 16, 23, 59, 59, 500, DateTimeKind.Utc), 1_095_379_199_500L },
                new object[]{ new DateTime(2009, 2, 13, 23, 31, 30, DateTimeKind.Utc), 1_234_567_890_000L },
                new object[]{ new DateTime(2019, 7, 24, 9, 21, 18, DateTimeKind.Utc), 1_563_960_078_000L },
                new object[]{ new DateTime(2033, 5, 18, 3, 33, 20, DateTimeKind.Utc), 2_000_000_000_000L },
            };
        }
    }
}
