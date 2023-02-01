using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
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
        public async Task TestSendToApplicationForDependencyErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                new AltReceiveProtocolInternal().SendToApplication(new DefaultQuasiHttpRequest()));
        }

        [Fact]
        public async Task TestSendToApplicationForRejectionOfNullResponses()
        {
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (req, reqEnv) =>
                {
                    return null;
                }
            };
            var instance = new AltReceiveProtocolInternal
            {
                Application = app,
            };
            await Assert.ThrowsAsync<ExpectationViolationException>(() =>
            {
                return instance.SendToApplication(new DefaultQuasiHttpRequest());
            });
        }

        [Fact]
        public async Task TestSendToApplication()
        {
            var request = new DefaultQuasiHttpRequest();
            var reqEnv = new Dictionary<string, object>
            {
                { "shared", true }
            };
            var expectedResponse = new ErrorQuasiHttpResponse();
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
                RequestEnvironment = reqEnv,
                Application = app
            };
            var actualResponse = await instance.SendToApplication(request);
            Assert.Equal(expectedResponse, actualResponse);
        }

        class ErrorQuasiHttpResponse : IQuasiHttpResponse
        {
            public string HttpStatusMessage => throw new NotImplementedException();

            public IDictionary<string, IList<string>> Headers => throw new NotImplementedException();

            public IQuasiHttpBody Body => throw new NotImplementedException();

            public int StatusCode => throw new NotImplementedException();

            public string HttpVersion => throw new NotImplementedException();

            public Task Close()
            {
                throw new NotImplementedException();
            }
        }
    }
}
