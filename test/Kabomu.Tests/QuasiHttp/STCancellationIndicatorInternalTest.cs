using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class STCancellationIndicatorInternalTest
    {
        [Fact]
        public void TestCancel()
        {
            var cancellationHandle = new STCancellationIndicatorInternal();

            Assert.False(cancellationHandle.Cancelled);
            cancellationHandle.Cancel();
            Assert.True(cancellationHandle.Cancelled);

            // check that subsequent cancellations have no effect
            cancellationHandle.Cancel();
            Assert.True(cancellationHandle.Cancelled);
        }
    }
}
