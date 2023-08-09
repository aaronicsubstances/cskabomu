using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Kabomu.Tests.Shared.Common;
using Kabomu.Common;
using System.IO;

namespace Kabomu.Tests.QuasiHttp
{
    public class DefaultQuasiHttpRequestTest
    {
        [Fact]
        public async Task TestRelease()
        {
            var instance = new DefaultQuasiHttpRequest();
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
