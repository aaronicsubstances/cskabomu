using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Server;
using Kabomu.Tests.Shared.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Server
{
    public class AltReceiveProtocolInternalTest
    {
        [Fact]
        public async Task TestReceiveForErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new AltReceiveProtocolInternal
                {
                    Request = new DefaultQuasiHttpRequest()
                };
                return instance.Receive();
            });

            var ex = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                var app = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async (req) =>
                    {
                        return null;
                    }
                };
                var instance = new AltReceiveProtocolInternal
                {
                    Application = app,
                    Request = new DefaultQuasiHttpRequest()
                };
                return instance.Receive();
            });
            Assert.Contains("no response", ex.Message);
        }

        [Fact]
        public async Task TestReceive()
        {
            var request = new DefaultQuasiHttpRequest();
            var reqEnv = new Dictionary<string, object>
            {
                { "shared", true }
            };
            var expectedResponse = new DefaultQuasiHttpResponse();
            IQuasiHttpRequest actualRequest = null;
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    return expectedResponse;
                }
            };
            var instance = new AltReceiveProtocolInternal
            {
                Request = request,
                Application = app
            };
            var actualResponse = await instance.Receive();
            Assert.Same(expectedResponse, actualResponse);
            Assert.Same(request, actualRequest);
        }
    }
}
