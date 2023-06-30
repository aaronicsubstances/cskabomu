using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Kabomu.Common;

namespace Kabomu.Tests.QuasiHttp
{
    public class DefaultQuasiHttpRequestTest
    {
        [Fact]
        public async Task TestCustomDispose()
        {
            var instance = new DefaultQuasiHttpRequest();
            await instance.CustomDispose();

            var disposed = false;
            instance.Body = new CustomReaderBackedBody(
                new LambdaBasedCustomReader(
                    (data, offset, length) =>
                    {
                        if (disposed)
                        {
                            throw new InvalidOperationException("disposed");
                        }
                        return Task.FromResult(0);
                    },
                    () =>
                    {
                        disposed = true;
                        return Task.CompletedTask;
                    }));
            instance.CancellationTokenSource = new CancellationTokenSource();
            int result = await instance.Body.AsReader().ReadBytes(new byte[1], 0, 1);
            Assert.Equal(0, result);
            Assert.False(instance.CancellationTokenSource.IsCancellationRequested);

            await instance.CustomDispose();
            Assert.True(instance.CancellationTokenSource.IsCancellationRequested);
            await Assert.ThrowsAsync<InvalidOperationException>(() => instance.Body.AsReader().ReadBytes(new byte[1], 0, 1));

            await instance.CustomDispose();
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
