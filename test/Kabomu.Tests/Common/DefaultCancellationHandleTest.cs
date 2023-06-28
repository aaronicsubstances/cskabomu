using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class DefaultCancellationHandleTest
    {
        [Fact]
        public void TestCancel()
        {
            var instance = new DefaultCancellationHandle();
            Assert.False(instance.IsCancelled);
            Assert.False(instance.IsCancelled); // repeat

            Assert.True(instance.Cancel());
            Assert.True(instance.IsCancelled);

            Assert.False(instance.Cancel());
            Assert.True(instance.IsCancelled);
        }
    }
}
