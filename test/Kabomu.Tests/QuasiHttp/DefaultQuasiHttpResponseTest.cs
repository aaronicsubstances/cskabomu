using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
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
    }
}
