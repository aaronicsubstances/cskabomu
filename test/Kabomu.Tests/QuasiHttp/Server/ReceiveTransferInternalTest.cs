using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;

namespace Kabomu.Tests.QuasiHttp.Server
{
    public class ReceiveTransferInternalTest
    {
        [Fact]
        public async Task TestStartProtocol1()
        {
            // arrange
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object()
            };
            var protocol = new HelperReceiveProtocol
            {
                ExpectedReceiveResult = new DefaultQuasiHttpResponse()
            };

            // act
            ReceiveTransferInternal actualInstance = null;
            var actual = await instance.StartProtocol(t =>
            {
                actualInstance = t;
                return protocol;
            });

            // assert
            Assert.Same(instance, actualInstance);
            Assert.Same(protocol.ExpectedReceiveResult, actual);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol2()
        {
            // arrange
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object(),
                IsAborted = true
            };
            var protocol = new HelperReceiveProtocol
            {
                ExpectedReceiveResult = new DefaultQuasiHttpResponse()
            };

            // act
            ReceiveTransferInternal actualInstance = null;
            var actual = await instance.StartProtocol(t =>
            {
                actualInstance = t;
                return protocol;
            });

            // assert
            Assert.Same(instance, actualInstance);
            Assert.Same(protocol.ExpectedReceiveResult, actual);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol3()
        {
            // arrange
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object()
            };
            var protocol = new HelperReceiveProtocol
            {
                ExpectedReceiveError = new NotImplementedException()
            };

            // act and assert error
            ReceiveTransferInternal actualInstance = null;
            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.StartProtocol(t =>
                {
                    actualInstance = t;
                    return protocol;
                });
            });

            // assert
            Assert.Same(instance, actualInstance);
        }

        [Fact]
        public async Task TestAbort1()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var protocol = new HelperReceiveProtocol();
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object(),
                Request = request,
                Protocol = protocol
            };
            var res = new DefaultQuasiHttpResponse
            {
                CancellationTokenSource = new CancellationTokenSource()
            };

            // act
            await instance.Abort(res);

            // assert
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.False(res.CancellationTokenSource.IsCancellationRequested);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestAbort2()
        {
            // arrange
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object()
            };
            var res = new DefaultQuasiHttpResponse();

            // act to verify no errors are raised with
            // the missing props
            await instance.Abort(res);
        }

        [Fact]
        public async Task TestAbort3()
        {
            // arrange
            var protocol = new HelperReceiveProtocol();
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object(),
                TimeoutId = new CancellationTokenSource(),
                Protocol = protocol,
                Request = request
            };
            var response = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("unbuffered"),
                CancellationTokenSource = new CancellationTokenSource()
            };

            // act
            await instance.Abort(response);

            // assert
            Assert.True(protocol.Cancelled);
            Assert.True(instance.TimeoutId.IsCancellationRequested);
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.False(response.CancellationTokenSource.IsCancellationRequested);
        }

        [Fact]
        public async Task TestAbort4()
        {
            // arrange
            var protocol = new HelperReceiveProtocol();
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object(),
                IsAborted = true,
                TimeoutId = new CancellationTokenSource(),
                Protocol = protocol
            };
            var response = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("unbuffered"),
                CancellationTokenSource = new CancellationTokenSource()
            };

            // act
            await instance.Abort(response);

            // assert
            Assert.False(protocol.Cancelled);
            Assert.False(instance.TimeoutId.IsCancellationRequested);
            Assert.False(request.CancellationTokenSource.IsCancellationRequested);
            Assert.True(response.CancellationTokenSource.IsCancellationRequested);
        }

        [Fact]
        public async Task TestAbort5()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var protocol = new HelperReceiveProtocol
            {
                ExpectedCancelError = new InvalidOperationException()
            };
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object(),
                Protocol = protocol,
                Request = request,
                TimeoutId = new CancellationTokenSource(),
            };
            var resCts = new CancellationTokenSource();
            var res = new DefaultQuasiHttpResponse
            {
                CancellationTokenSource = resCts,
                Body = new StringBody("deal")
            };

            // act
            await instance.Abort(res);

            // assert
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.True(instance.TimeoutId.IsCancellationRequested);
            Assert.True(protocol.Cancelled);
            Assert.False(resCts.IsCancellationRequested);
        }

        [Fact]
        public async Task TestAbort6()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var protocol = new HelperReceiveProtocol
            {
                ExpectedCancelError = new InvalidOperationException()
            };
            var instance = new ReceiveTransferInternal
            {
                Mutex = new object(),
                Protocol = protocol,
                Request = request,
                TimeoutId = new CancellationTokenSource()
            };
            var res = new DefaultQuasiHttpResponse();

            // act
            await instance.Abort(res);

            // assert
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.True(instance.TimeoutId.IsCancellationRequested);
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
