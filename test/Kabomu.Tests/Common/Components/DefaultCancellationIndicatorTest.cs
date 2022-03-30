using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Components
{
    public class DefaultCancellationIndicatorTest
    {
        [Fact]
        public void TestCancel()
        {
            var cancellationHandle = new DefaultCancellationIndicator();

            Assert.False(cancellationHandle.Cancelled);
            cancellationHandle.Cancel();
            Assert.True(cancellationHandle.Cancelled);

            // check that subsequent cancellations have no effect
            cancellationHandle.Cancel();
            Assert.True(cancellationHandle.Cancelled);
        }
    }
}
