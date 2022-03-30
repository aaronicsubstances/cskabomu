using Kabomu.Common.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Internals
{
    public class STCancellationIndicatorTest
    {
        [Fact]
        public void TestCancel()
        {
            var cancellationHandle = new STCancellationIndicator();

            Assert.False(cancellationHandle.Cancelled);
            cancellationHandle.Cancel();
            Assert.True(cancellationHandle.Cancelled);

            // check that subsequent cancellations have no effect
            cancellationHandle.Cancel();
            Assert.True(cancellationHandle.Cancelled);
        }
    }
}
