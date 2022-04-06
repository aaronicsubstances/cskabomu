using Kabomu.Common.Helpers;
using Kabomu.Common.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Internals
{
    public class STMessageIdGeneratorTest
    {
        [Fact]
        public void TestDeterministicNextIdGeneration()
        {
            var instance = new STMessageIdGenerator(0);
            // use int32 instead of int64 so we can use repeated calls to 
            // Java's (new Random(0)).next()) to generate expected
            // test results.
            var expectedTop = new int[] { -1268774284, 1362668399, -881149874, 1891536193, -906589512,
                -264609693, 1891105842, 620973397, -1170322628, -1485967209 };
            var actualTop = new int[expectedTop.Length];
            for (int i = 0; i < actualTop.Length; i++)
            {
                actualTop[i] = (int)instance.NextId();
            }
            Assert.Equal(expectedTop, actualTop);
        }

        [Fact]
        public void TestRandomNextIdGeneration()
        {
            var instance = new STMessageIdGenerator(DateTimeUtils.UnixTimeMillis);
            var actual = new long[100];
            for (int i = 0; i < actual.Length; i++)
            {
                actual[i] = instance.NextId();
            }

            // due to randomness involved, just check that it can generates unique ids without errors.
            for (int i = 0; i < actual.Length; i++)
            {
                for (int j = i + 1; j < actual.Length; j++)
                {
                    if (actual[i] == actual[j])
                    {
                        Assert.True(false, $"not pseudo random enough: {string.Join(", ", actual)}");
                    }
                }
            }
        }
    }
}
