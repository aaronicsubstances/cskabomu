using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class DefaultQuasiHttpResponseTest
    {
        [Fact]
        public async Task TestClose()
        {
            var instance = new DefaultQuasiHttpResponse();
            await instance.Close();

            instance.Body = new ByteBufferBody(new byte[0]);
            instance.CancellationTokenSource = new CancellationTokenSource();
            int result = await instance.Body.ReadBytes(new byte[1], 0, 1);
            Assert.Equal(0, result);

            await instance.Close();
            await Assert.ThrowsAsync<EndOfReadException>(() => instance.Body.ReadBytes(new byte[1], 0, 1));

            await instance.Close();
        }

        [Fact]
        public void TestClassConstants()
        {
            Assert.Equal(200, DefaultQuasiHttpResponse.StatusCodeOk);
            Assert.Equal(500, DefaultQuasiHttpResponse.StatusCodeServerError);
            Assert.Equal(400, DefaultQuasiHttpResponse.StatusCodeClientError);
            Assert.Equal(400, DefaultQuasiHttpResponse.StatusCodeClientErrorBadRequest);
            Assert.Equal(401, DefaultQuasiHttpResponse.StatusCodeClientErrorUnauthorized);
            Assert.Equal(403, DefaultQuasiHttpResponse.StatusCodeClientErrorForbidden);
            Assert.Equal(404, DefaultQuasiHttpResponse.StatusCodeClientErrorNotFound);
            Assert.Equal(413, DefaultQuasiHttpResponse.StatusCodeClientErrorPayloadTooLarge);
            Assert.Equal(414, DefaultQuasiHttpResponse.StatusCodeClientErrorURITooLong);
            Assert.Equal(415, DefaultQuasiHttpResponse.StatusCodeClientErrorUnsupportedMediaType);
            Assert.Equal(422, DefaultQuasiHttpResponse.StatusCodeClientErrorUnprocessableEntity);
            Assert.Equal(429, DefaultQuasiHttpResponse.StatusCodeClientErrorTooManyRequests);
        }
    }
}
