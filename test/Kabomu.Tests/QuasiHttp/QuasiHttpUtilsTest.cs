using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class QuasiHttpUtilsTest
    {
        [Fact]
        public void TestClassConstants()
        {
            Assert.Equal("CONNECT", QuasiHttpUtils.MethodConnect);
            Assert.Equal("DELETE", QuasiHttpUtils.MethodDelete);
            Assert.Equal("GET", QuasiHttpUtils.MethodGet);
            Assert.Equal("HEAD", QuasiHttpUtils.MethodHead);
            Assert.Equal("OPTIONS", QuasiHttpUtils.MethodOptions);
            Assert.Equal("PATCH", QuasiHttpUtils.MethodPatch);
            Assert.Equal("POST", QuasiHttpUtils.MethodPost);
            Assert.Equal("PUT", QuasiHttpUtils.MethodPut);
            Assert.Equal("TRACE", QuasiHttpUtils.MethodTrace);

            Assert.Equal(200, QuasiHttpUtils.StatusCodeOk);
            Assert.Equal(500, QuasiHttpUtils.StatusCodeServerError);
            Assert.Equal(400, QuasiHttpUtils.StatusCodeClientErrorBadRequest);
            Assert.Equal(401, QuasiHttpUtils.StatusCodeClientErrorUnauthorized);
            Assert.Equal(403, QuasiHttpUtils.StatusCodeClientErrorForbidden);
            Assert.Equal(404, QuasiHttpUtils.StatusCodeClientErrorNotFound);
            Assert.Equal(405, QuasiHttpUtils.StatusCodeClientErrorMethodNotAllowed);
            Assert.Equal(413, QuasiHttpUtils.StatusCodeClientErrorPayloadTooLarge);
            Assert.Equal(414, QuasiHttpUtils.StatusCodeClientErrorURITooLong);
            Assert.Equal(415, QuasiHttpUtils.StatusCodeClientErrorUnsupportedMediaType);
            Assert.Equal(422, QuasiHttpUtils.StatusCodeClientErrorUnprocessableEntity);
            Assert.Equal(429, QuasiHttpUtils.StatusCodeClientErrorTooManyRequests);
        }
    }
}
