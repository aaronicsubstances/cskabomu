using Kabomu.Common;
using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class ProtocolUtilsTest
    {
        [Fact]
        public void TestCreateCancellationEnquirer()
        {
            var cancellationIndicator = new STCancellationIndicator();
            var instance = ProtocolUtils.CreateCancellationEnquirer(new TestEventLoopApi(), cancellationIndicator);

            var cbCalled = false;
            instance.Invoke(cancelled =>
            {
                Assert.False(cancelled);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            cancellationIndicator.Cancel();
            cbCalled = false;
            instance.Invoke(cancelled =>
            {
                Assert.True(cancelled);
                cbCalled = true;
            });
            Assert.True(cbCalled);
        }
    }
}
