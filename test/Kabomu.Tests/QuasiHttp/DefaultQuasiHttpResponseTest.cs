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
    }
}
