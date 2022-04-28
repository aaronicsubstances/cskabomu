using Kabomu.Common;
using Kabomu.QuasiHttp.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Internals
{
    public class STRequestIdGeneratorTest
    {
        [Fact]
        public void TestDeterministicNextIdGeneration()
        {
            var instance = new STRequestIdGenerator(0);
            var expectedTop = new int[] { 16_807, 282_475_249, 1_622_650_073, 984_943_658, 1_144_108_930,
                470_211_272, 101_027_544, 1_457_850_878, 1_458_777_923, 2_007_237_709 };
            var actualTop = new int[expectedTop.Length];
            for (int i = 0; i < actualTop.Length; i++)
            {
                actualTop[i] = instance.NextId();
            }
            Assert.Equal(expectedTop, actualTop);
        }

        [Fact]
        public void TestRandomNextIdGeneration()
        {
            var instance = new STRequestIdGenerator(DateTimeUtils.UnixTimeMillis);
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
