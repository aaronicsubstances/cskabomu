using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared.QuasiHttp;

namespace Kabomu.Tests.QuasiHttp.Server
{
    public class ReceiveTransferInternalTest
    {
        [Fact]
        public async Task TestStartProtocol1()
        {
            // arrange
            var protocol = new HelperReceiveProtocol
            {
                ExpectedReceiveResult = new DefaultQuasiHttpResponse()
            };
            var instance = new ReceiveTransferInternal
            {
                Protocol = protocol
            };

            // act
            var actual = await instance.StartProtocol();

            // assert
            Assert.Same(protocol.ExpectedReceiveResult, actual);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol2()
        {
            // arrange
            var protocol = new HelperReceiveProtocol
            {
                ExpectedReceiveResult = new DefaultQuasiHttpResponse()
            };
            var instance = new ReceiveTransferInternal
            {
                Protocol = protocol
            };
            instance.TrySetAborted();

            // act
            var actual = await instance.StartProtocol();

            // assert
            Assert.Same(protocol.ExpectedReceiveResult, actual);
            Assert.False(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol3()
        {
            // arrange
            var protocol = new HelperReceiveProtocol
            {
                ExpectedReceiveError = new NotImplementedException()
            };
            var instance = new ReceiveTransferInternal
            {
                Protocol = protocol
            };

            // act and assert error
            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.StartProtocol();
            });
        }

        [Fact]
        public async Task TestAbort1()
        {
            // arrange
            var protocol = new HelperReceiveProtocol();
            var instance = new ReceiveTransferInternal
            {
                Protocol = protocol
            };
            var responseReleaseCallCount = 0;
            var res = new ConfigurableQuasiHttpResponse
            {
                ReleaseFunc = async () =>
                {
                    responseReleaseCallCount++;
                }
            };

            // act
            await instance.Abort(res);

            // assert
            Assert.Equal(0, responseReleaseCallCount);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestAbort2()
        {
            // arrange
            var instance = new ReceiveTransferInternal();
            var res = new DefaultQuasiHttpResponse();

            // act to verify no errors are raised with
            // the missing props
            await instance.Abort(res);
        }

        [Fact]
        public async Task TestAbort3()
        {
            // arrange
            var instance = new ReceiveTransferInternal();
            instance.TrySetAborted();

            // act to verify no errors are raised with
            // the missing props
            await instance.Abort(null);
        }

        [Fact]
        public async Task TestAbort4()
        {
            // arrange
            var protocol = new HelperReceiveProtocol();
            var instance = new ReceiveTransferInternal
            {
                TimeoutId = new CancellablePromiseInternal<IQuasiHttpResponse>
                {
                    CancellationTokenSource = new CancellationTokenSource()
                },
                Protocol = protocol,
            };
            var responseReleaseCallCount = 0;
            var response = new ConfigurableQuasiHttpResponse
            {
                Body = new StringBody("unbuffered"),
                ReleaseFunc = async () =>
                {
                    responseReleaseCallCount++;
                }
            };

            // act
            await instance.Abort(response);

            // assert
            Assert.True(protocol.Cancelled);
            Assert.True(instance.TimeoutId.IsCancellationRequested());
            Assert.Equal(0, responseReleaseCallCount);
        }

        [Fact]
        public async Task TestAbort5()
        {
            // arrange
            var protocol = new HelperReceiveProtocol();
            var instance = new ReceiveTransferInternal
            {
                TimeoutId = new CancellablePromiseInternal<IQuasiHttpResponse>
                {
                    CancellationTokenSource = new CancellationTokenSource()
                },
                Protocol = protocol
            };
            instance.TrySetAborted();
            var responseReleaseCallCount = 0;
            var response = new ConfigurableQuasiHttpResponse
            {
                Body = new StringBody("unbuffered"),
                ReleaseFunc = async () =>
                {
                    responseReleaseCallCount++;
                    throw new Exception("should be ignored");
                }
            };

            // act
            await instance.Abort(response);

            // assert
            Assert.False(protocol.Cancelled);
            Assert.False(instance.TimeoutId.IsCancellationRequested());
            Assert.Equal(1, responseReleaseCallCount);
        }

        [Fact]
        public async Task TestAbort6()
        {
            // arrange
            var protocol = new HelperReceiveProtocol
            {
                ExpectedCancelError = new InvalidOperationException()
            };
            var instance = new ReceiveTransferInternal
            {
                Protocol = protocol,
                TimeoutId = new CancellablePromiseInternal<IQuasiHttpResponse>
                {
                    CancellationTokenSource = new CancellationTokenSource()
                },
            };
            var res = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("deal")
            };

            // act
            await instance.Abort(res);

            // assert
            Assert.True(instance.TimeoutId.IsCancellationRequested());
            Assert.True(protocol.Cancelled);
        }

        class HelperReceiveProtocol : IReceiveProtocolInternal
        {
            public bool Cancelled { get; set; }
            public Exception ExpectedCancelError { get; set; }
            public Exception ExpectedReceiveError { get; set; }
            public IQuasiHttpResponse ExpectedReceiveResult { get; set; }

            public Task Cancel()
            {
                Cancelled = true;
                if (ExpectedCancelError != null)
                {
                    return Task.FromException(ExpectedCancelError);
                }
                return Task.CompletedTask;
            }

            public Task<IQuasiHttpResponse> Receive()
            {
                if (ExpectedReceiveError != null)
                {
                    return Task.FromException<IQuasiHttpResponse>(ExpectedReceiveError);
                }
                return Task.FromResult(ExpectedReceiveResult);
            }
        }
    }
}
