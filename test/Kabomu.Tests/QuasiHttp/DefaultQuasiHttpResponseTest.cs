using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class DefaultQuasiHttpResponseTest
    {
        [Fact]
        public async Task TestRelease()
        {
            var instance = new DefaultQuasiHttpResponse();
            await instance.Release();

            var stream = new MemoryStream();
            instance.Body = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => stream,
                ReleaseFunc = async () => await stream.DisposeAsync()
            };
            int result = await IOUtils.ReadBytes(instance.Body.AsReader(),
                new byte[1], 0, 1);
            Assert.Equal(0, result);

            await instance.Release();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                IOUtils.ReadBytes(instance.Body.AsReader(), new byte[1], 0, 1));

            await instance.Release();
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
