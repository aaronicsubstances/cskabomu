using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class DefaultQuasiHttpRequestTest
    {
        [Fact]
        public void TestClassConstants()
        {
            Assert.Equal("CONNECT", DefaultQuasiHttpRequest.MethodConnect);
            Assert.Equal("DELETE", DefaultQuasiHttpRequest.MethodDelete);
            Assert.Equal("GET", DefaultQuasiHttpRequest.MethodGet);
            Assert.Equal("HEAD", DefaultQuasiHttpRequest.MethodHead);
            Assert.Equal("OPTIONS", DefaultQuasiHttpRequest.MethodOptions);
            Assert.Equal("PATCH", DefaultQuasiHttpRequest.MethodPatch);
            Assert.Equal("POST", DefaultQuasiHttpRequest.MethodPost);
            Assert.Equal("PUT", DefaultQuasiHttpRequest.MethodPut);
            Assert.Equal("TRACE", DefaultQuasiHttpRequest.MethodTrace);
        }
    }
}
