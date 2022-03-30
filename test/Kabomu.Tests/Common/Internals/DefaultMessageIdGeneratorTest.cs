using Kabomu.Common.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Internals
{
    public class DefaultMessageIdGeneratorTest
    {
        [Fact]
        public void TestNextId()
        {
            var instance = new DefaultMessageIdGenerator();
            // due to randomness involved, just check that it can generates ids in sequence without errors.
            var first = instance.NextId();
            var second = instance.NextId();
            Assert.Equal(1, second - first);
        }
    }
}
