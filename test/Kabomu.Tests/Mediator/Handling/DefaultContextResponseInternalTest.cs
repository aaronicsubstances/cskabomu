using Kabomu.Mediator;
using Kabomu.Mediator.Handling;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Mediator.Handling
{
    public class DefaultContextResponseInternalTest
    {
        [Fact]
        public async Task Test1()
        {
            var rawResponse = new DefaultQuasiHttpResponse();
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var instance = new DefaultContextResponseInternal(rawResponse, responseTransmitter);
            Assert.Same(rawResponse, instance.RawResponse);

            // check for initial conditions.
            Assert.Equal(0, instance.StatusCode);
            Assert.False(instance.IsSuccessStatusCode);
            Assert.False(instance.IsClientErrorStatusCode);
            Assert.False(instance.IsServerErrorStatusCode);
            Assert.Null(instance.Body);
            Assert.NotNull(instance.Headers);
            Assert.Empty(instance.Headers.GetNames());
            Assert.Null(rawResponse.Headers);

            // modify response.
            instance.SetStatusCode(DefaultQuasiHttpResponse.StatusCodeClientErrorMethodNotAllowed);
            Assert.Equal(405, instance.StatusCode);
            Assert.Equal(405, rawResponse.StatusCode);
            Assert.False(instance.IsSuccessStatusCode);
            Assert.True(instance.IsClientErrorStatusCode);
            Assert.False(instance.IsServerErrorStatusCode);

            var bodyToUse = new ByteBufferBody(new byte[0]);
            instance.SetBody(bodyToUse);
            Assert.Same(bodyToUse, instance.Body);
            Assert.Same(bodyToUse, rawResponse.Body);

            instance.Headers.Add("Allow", "GET");
            instance.Headers.Add("Allow", "POST");
            ComparisonUtils.AssertSetEqual(new List<string> { "Allow" },
                instance.Headers.GetNames());
            Assert.Equal(new List<string> { "GET", "POST" },
                instance.Headers.GetAll("Allow"));
            Assert.NotNull(rawResponse.Headers);
            Assert.Equal(new List<string> { "GET", "POST" },
                rawResponse.Headers["Allow"]);

            instance.Send();
            // check that response transmitter was used.
            Assert.Equal(rawResponse, await responseTransmitter.Task);
        }

        [Fact]
        public async Task Test2()
        {
            var rawResponse = new DefaultQuasiHttpResponse();
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var instance = new DefaultContextResponseInternal(rawResponse, responseTransmitter);
            Assert.Same(rawResponse, instance.RawResponse);

            // check for initial conditions.
            Assert.Equal(0, instance.StatusCode);
            Assert.False(instance.IsSuccessStatusCode);
            Assert.False(instance.IsClientErrorStatusCode);
            Assert.False(instance.IsServerErrorStatusCode);
            Assert.Null(instance.Body);
            Assert.NotNull(instance.Headers);
            Assert.Empty(instance.Headers.GetNames());
            Assert.Null(rawResponse.Headers);

            // modify response.
            instance.SetSuccessStatusCode();
            Assert.Equal(200, instance.StatusCode);
            Assert.Equal(200, rawResponse.StatusCode);
            Assert.True(instance.IsSuccessStatusCode);
            Assert.False(instance.IsClientErrorStatusCode);
            Assert.False(instance.IsServerErrorStatusCode);

            var bodyToUse = new ByteBufferBody(new byte[0]);
            instance.SetBody(bodyToUse);
            Assert.Same(bodyToUse, instance.Body);
            Assert.Same(bodyToUse, rawResponse.Body);

            var sent = instance.TrySend(() =>
            {
                instance.SetBody(null);
            });
            Assert.True(sent);
            Assert.Null(instance.Body);
            Assert.Null(rawResponse.Body);
            // check that response transmitter was used.
            Assert.Equal(rawResponse, await responseTransmitter.Task);

            // check that future sending attempts fail.
            Assert.Throws<ResponseCommittedException>(() => instance.Send());
            Assert.False(instance.TrySend());
            Assert.False(instance.TrySend(null));
            Assert.False(instance.TrySend(() =>
            {
                rawResponse.StatusCode = 111;
                rawResponse.Body = bodyToUse;
            }));
            // check that no changes were made after response commit.
            Assert.Equal(200, rawResponse.StatusCode);
            Assert.Null(rawResponse.Body);
            Assert.Null(rawResponse.Headers);
            Assert.Null(rawResponse.HttpStatusMessage);
            Assert.Null(rawResponse.HttpVersion);
        }

        [Fact]
        public async Task Test3()
        {
            var rawResponse = new DefaultQuasiHttpResponse();
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var instance = new DefaultContextResponseInternal(rawResponse, responseTransmitter);
            Assert.Same(rawResponse, instance.RawResponse);

            // modify response.
            instance.SetClientErrorStatusCode();
            Assert.Equal(400, instance.StatusCode);
            Assert.Equal(400, rawResponse.StatusCode);
            Assert.False(instance.IsSuccessStatusCode);
            Assert.True(instance.IsClientErrorStatusCode);
            Assert.False(instance.IsServerErrorStatusCode);

            IQuasiHttpBody bodyToUse = new ByteBufferBody(new byte[0]);
            instance.SetBody(bodyToUse);
            Assert.Same(bodyToUse, instance.Body);
            Assert.Same(bodyToUse, rawResponse.Body);

            bodyToUse = new StringBody("lear");
            var expectedRawHeaders = new Dictionary<string, IList<string>>
            {
                { "test", new string[]{"v"} }
            };
            var sent = instance.TrySend(() =>
            {
                rawResponse.StatusCode = 22;
                rawResponse.Body = bodyToUse;
                rawResponse.HttpStatusMessage = "t";
                rawResponse.HttpVersion = "3";
                rawResponse.Headers = expectedRawHeaders;
            });
            Assert.True(sent);
            Assert.Equal(bodyToUse, instance.Body);
            Assert.Equal(bodyToUse, rawResponse.Body);
            // check that response transmitter was used.
            Assert.Equal(rawResponse, await responseTransmitter.Task);
            // check the changes made.
            Assert.Equal(22, rawResponse.StatusCode);
            Assert.Same(bodyToUse, rawResponse.Body);
            Assert.Equal(expectedRawHeaders, rawResponse.Headers);
            Assert.Equal("t", rawResponse.HttpStatusMessage);
            Assert.Equal("3", rawResponse.HttpVersion);
        }

        [Fact]
        public async Task Test4()
        {
            var rawResponse = new DefaultQuasiHttpResponse();
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var instance = new DefaultContextResponseInternal(rawResponse, responseTransmitter);
            Assert.Same(rawResponse, instance.RawResponse);

            // modify response.
            instance.SetServerErrorStatusCode();
            Assert.Equal(500, instance.StatusCode);
            Assert.Equal(500, rawResponse.StatusCode);
            Assert.False(instance.IsSuccessStatusCode);
            Assert.False(instance.IsClientErrorStatusCode);
            Assert.True(instance.IsServerErrorStatusCode);

            IQuasiHttpBody bodyToUse = new ByteBufferBody(new byte[0]);
            instance.SetBody(bodyToUse);
            Assert.Same(bodyToUse, instance.Body);
            Assert.Same(bodyToUse, rawResponse.Body);

            bodyToUse = new StringBody("n");
            bool sent = instance.TrySend(() =>
            {
                rawResponse.Body = bodyToUse;
            });
            Assert.True(sent);
            Assert.Same(bodyToUse, instance.Body);
            Assert.Same(bodyToUse, rawResponse.Body);
            // check that response transmitter was used.
            Assert.Equal(rawResponse, await responseTransmitter.Task);

            // check that future sending attempts fail.
            Assert.Throws<ResponseCommittedException>(() => instance.Send());
            Assert.False(instance.TrySend());
            Assert.False(instance.TrySend(() =>
            {
                rawResponse.Body = bodyToUse;
            }));
            Assert.False(instance.TrySend(null));
            // test that body remains unchanged.
            Assert.Same(bodyToUse, instance.Body);
        }

        [Fact]
        public async Task Test5()
        {
            var rawResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "year", new string[]{ "2022" } },
                    { "hour", new string[]{ "10", "14", "2" } }
                }
            };
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var instance = new DefaultContextResponseInternal(rawResponse, responseTransmitter);
            Assert.Same(rawResponse, instance.RawResponse);

            ComparisonUtils.AssertSetEqual(new List<string> { "year", "hour" },
                instance.Headers.GetNames());
            Assert.Equal(new List<string> { "2022" },
                instance.Headers.GetAll("year"));
            Assert.Equal(new List<string> { "10", "14", "2" },
                rawResponse.Headers["hour"]);

            // modify response.
            instance.SetStatusCode(DefaultQuasiHttpResponse.StatusCodeClientErrorNotFound);
            Assert.Equal(404, instance.StatusCode);
            Assert.Equal(404, rawResponse.StatusCode);
            Assert.False(instance.IsSuccessStatusCode);
            Assert.True(instance.IsClientErrorStatusCode);
            Assert.False(instance.IsServerErrorStatusCode);

            bool sent = instance.TrySend();
            Assert.True(sent);
            Assert.Null(instance.Body);
            Assert.Null(rawResponse.Body);
            // check that response transmitter was used.
            Assert.Equal(rawResponse, await responseTransmitter.Task);

            // check that future sending attempts fail.
            Assert.Throws<ResponseCommittedException>(() => instance.Send());
            Assert.False(instance.TrySend());
            Assert.False(instance.TrySend(null));
            Assert.False(instance.TrySend(() =>
            {
                rawResponse.Body = new ByteBufferBody(new byte[0]);
            }));
            // test that body remains unchanged.
            Assert.Null(instance.Body);
        }

        [Fact]
        public void TestReturnOfBuilderMethods()
        {
            var rawResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "year", new string[]{ "2022" } },
                    { "hour", new string[]{ "10", "14", "2" } }
                }
            };
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var instance = new DefaultContextResponseInternal(rawResponse, responseTransmitter);
            Assert.Same(rawResponse, instance.RawResponse);

            Assert.Same(instance, instance.SetSuccessStatusCode());
            Assert.Same(instance, instance.SetClientErrorStatusCode());
            Assert.Same(instance, instance.SetServerErrorStatusCode());
            Assert.Same(instance, instance.SetBody(new StringBody("")));
            Assert.Same(instance, instance.SetBody(null));
            Assert.Same(instance, instance.SetStatusCode(401));
            Assert.Same(instance, instance.SetStatusCode(0));
        }

        [Theory]
        [MemberData(nameof(CreateTestStatusCodeBooleanIndicatorsData))]
        public void TestStatusCodeBooleanIndicators(int statusCode, bool expectedIsSuccess,
            bool expectedIsClientError, bool expectedIsServerError)
        {
            var rawResponse = new DefaultQuasiHttpResponse();
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var instance = new DefaultContextResponseInternal(rawResponse, responseTransmitter);
            Assert.Same(rawResponse, instance.RawResponse);

            Assert.False(instance.IsSuccessStatusCode);
            Assert.False(instance.IsClientErrorStatusCode);
            Assert.False(instance.IsSuccessStatusCode);

            instance.SetStatusCode(statusCode);

            Assert.Equal(expectedIsSuccess, instance.IsSuccessStatusCode);
            Assert.Equal(expectedIsClientError, instance.IsClientErrorStatusCode);
            Assert.Equal(expectedIsServerError, instance.IsServerErrorStatusCode);
        }

        public static List<object[]> CreateTestStatusCodeBooleanIndicatorsData()
        {
            return new List<object[]>
            {
                new object[]{ 0, false, false, false },
                new object[]{ 199, false, false, false },
                new object[]{ 200, true, false, false },
                new object[]{ 201, true, false, false },
                new object[]{ 202, true, false, false },
                new object[]{ 298, true, false, false },
                new object[]{ 299, true, false, false },
                new object[]{ 300, false, false, false },

                new object[]{ 301, false, false, false },
                new object[]{ 399, false, false, false },
                new object[]{ 400, false, true, false },
                new object[]{ 401, false, true, false },
                new object[]{ 402, false, true, false },
                new object[]{ 498, false, true, false },
                new object[]{ 499, false, true, false },

                new object[]{ 500, false, false, true },
                new object[]{ 501, false, false, true },
                new object[]{ 502, false, false, true },
                new object[]{ 503, false, false, true },
                new object[]{ 598, false, false, true },
                new object[]{ 599, false, false, true },

                new object[]{ 600, false, false, false },
                new object[]{ 601, false, false, false },
            };
        }
    }
}
