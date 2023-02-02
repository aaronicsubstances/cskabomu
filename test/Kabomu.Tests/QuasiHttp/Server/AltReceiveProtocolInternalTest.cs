using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Server;
using Kabomu.Tests.Shared;
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
        public async Task TestReceiveForDependencyErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new AltReceiveProtocolInternal
                {
                    Request = new DefaultQuasiHttpRequest()
                };
                return instance.Receive();
            });
        }

        [Fact]
        public async Task TestReceiveForRejectionOfNullResponses()
        {
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (req, reqEnv) =>
                {
                    return null;
                }
            };
            await Assert.ThrowsAsync<ExpectationViolationException>(() =>
            {
                var instance = new AltReceiveProtocolInternal
                {
                    Application = app,
                    Request = new DefaultQuasiHttpRequest()
                };
                return instance.Receive();
            });
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
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (actualRequest, actualReqEnv) =>
                {
                    Assert.Equal(request, actualRequest);
                    Assert.Equal(reqEnv, actualReqEnv);
                    return expectedResponse;
                }
            };
            var instance = new AltReceiveProtocolInternal
            {
                Request = request,
                RequestEnvironment = reqEnv,
                Application = app
            };
            var actualResponse = await instance.Receive();
            Assert.Same(expectedResponse, actualResponse);
        }
    }
}
