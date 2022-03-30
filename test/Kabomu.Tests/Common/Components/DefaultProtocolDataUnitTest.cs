using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Components
{
    public class DefaultProtocolDataUnitTest
    {
        [Theory]
        [InlineData(0, false)]
        [InlineData(8, false)]
        [InlineData(64, false)]
        [InlineData(128, true)]
        [InlineData(192, true)]
        public void TestIsStartedAtReceiverFlagPresent(byte flags, bool expected)
        {
            var actual = DefaultProtocolDataUnit.IsStartedAtReceiverFlagPresent(flags);
            Assert.Equal(actual, expected);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(8, false)]
        [InlineData(64, true)]
        [InlineData(128, false)]
        [InlineData(192, true)]
        public void TestIsHasMoreFlagPresent(byte flags, bool expected)
        {
            var actual = DefaultProtocolDataUnit.IsHasMoreFlagPresent(flags);
            Assert.Equal(actual, expected);
        }

        [Theory]
        [InlineData(false, false, 0)]
        [InlineData(false, true, 64)]
        [InlineData(true, false, 128)]
        [InlineData(true, true, 192)]
        public void TestComputeFlags(bool startedAtReceiver, bool hasMore, byte expected)
        {
            var actual = DefaultProtocolDataUnit.ComputeFlags(startedAtReceiver, hasMore);
            Assert.Equal(actual, expected);
        }
    }
}
