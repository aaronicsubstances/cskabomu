using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class EntityBodyUtilsInternalTest
    {
        [Fact]
        public void Test()
        {
            EntityBodyUtilsInternal.ThrowIfReadCancelled(false);
            EntityBodyUtilsInternal.ThrowIfReadCancelled(new DefaultCancellationHandle());
            var actualEx = Assert.Throws<EndOfReadException>(() => EntityBodyUtilsInternal.ThrowIfReadCancelled(true));
            Assert.Contains("end of read", actualEx.Message);
            var cancellationHandle = new DefaultCancellationHandle();
            cancellationHandle.Cancel();
            actualEx = Assert.Throws<EndOfReadException>(() => EntityBodyUtilsInternal.ThrowIfReadCancelled(cancellationHandle));
            Assert.Contains("end of read", actualEx.Message);
        }
    }
}
