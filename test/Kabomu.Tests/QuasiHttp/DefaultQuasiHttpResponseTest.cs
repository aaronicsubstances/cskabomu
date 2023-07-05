using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
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
        public async Task TestCustomDispose()
        {
            var instance = new DefaultQuasiHttpResponse();
            await instance.CustomDispose();

            instance.Body = new CustomReaderBackedBody(new DemoCustomReaderWriter());
            instance.CancellationTokenSource = new CancellationTokenSource();
            int result = await instance.Body.AsReader().ReadBytes(new byte[1], 0, 1);
            Assert.Equal(0, result);
            Assert.False(instance.CancellationTokenSource.IsCancellationRequested);

            await instance.CustomDispose();
            Assert.True(instance.CancellationTokenSource.IsCancellationRequested);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                instance.Body.AsReader().ReadBytes(new byte[1], 0, 1));

            await instance.CustomDispose();
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
