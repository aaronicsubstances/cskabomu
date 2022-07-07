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
            EntityBodyUtilsInternal.ThrowIfReadCancelled(new CancellationTokenSource());
            var actualEx = Assert.ThrowsAny<Exception>(() => EntityBodyUtilsInternal.ThrowIfReadCancelled(true));
            Assert.Contains("end of read", actualEx.Message);
            var cts = new CancellationTokenSource();
            cts.Cancel();
            actualEx = Assert.ThrowsAny<Exception>(() => EntityBodyUtilsInternal.ThrowIfReadCancelled(cts));
            Assert.Contains("end of read", actualEx.Message);
        }
    }
}
