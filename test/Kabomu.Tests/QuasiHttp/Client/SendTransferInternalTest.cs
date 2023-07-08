using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Client
{
    public class SendTransferInternalTest
    {
        [Fact]
        public async Task TestStartProtocol1()
        {
            // arrange
            var instance = new SendTransferInternal
            {
                Mutex = new object()
            };
            var protocol = new HelperSendProtocol
            {
                ExpectedSendResult = new ProtocolSendResultInternal()
            };

            // act
            SendTransferInternal actualInstance = null;
            var actual = await instance.StartProtocol(t =>
            {
                actualInstance = t;
                return protocol;
            });

            // assert
            Assert.Same(instance, actualInstance);
            Assert.Same(protocol.ExpectedSendResult, actual);
            Assert.False(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol2()
        {
            // arrange
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                IsAborted = true
            };
            var protocol = new HelperSendProtocol
            {
                ExpectedSendResult = new ProtocolSendResultInternal()
            };

            // act
            SendTransferInternal actualInstance = null;
            var actual = await instance.StartProtocol(t =>
            {
                actualInstance = t;
                return protocol;
            });

            // assert
            Assert.Same(instance, actualInstance);
            Assert.Null(actual);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol3()
        {
            // arrange
            var instance = new SendTransferInternal
            {
                Mutex = new object()
            };
            var protocol = new HelperSendProtocol
            {
                ExpectedSendError = new NotImplementedException()
            };

            // act and assert error
            SendTransferInternal actualInstance = null;
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
            var protocol = new HelperSendProtocol();
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                Request = request,
                Protocol = protocol
            };
            Exception cancellationError = null;
            var resCts = new CancellationTokenSource();
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                ResponseBufferingApplied = true,
                Response = new DefaultQuasiHttpResponse
                {
                    Body = new StringBody("ice"),
                    CancellationTokenSource = resCts
                }
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.False(resCts.IsCancellationRequested);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestAbort2()
        {
            // arrange
            var protocol = new HelperSendProtocol();
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                Protocol = protocol
            };
            Exception cancellationError = null;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal();

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestAbort3()
        {
            // arrange
            var protocol = new HelperSendProtocol();
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>(),
                Protocol = protocol
            };
            Exception cancellationError = new InvalidOperationException();
            ProtocolSendResultInternal res = new ProtocolSendResultInternal();

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.True(protocol.Cancelled);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return instance.CancellationTcs.Task;
            });
        }

        [Fact]
        public async Task TestAbort4()
        {
            // arrange
            var protocol = new HelperSendProtocol();
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                TimeoutId = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>(),
                Protocol = protocol,
                Request = request
            };
            Exception cancellationError = null;
            var resCts = new CancellationTokenSource();
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                Response = new DefaultQuasiHttpResponse
                {
                    Body = new StringBody("unbuffered"),
                    CancellationTokenSource = resCts
                }
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.False(protocol.Cancelled);
            Assert.True(instance.TimeoutId.IsCancellationRequested);
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.False(resCts.IsCancellationRequested);
            Assert.Same(res, await instance.CancellationTcs.Task);
        }

        [Fact]
        public async Task TestAbort5()
        {
            // arrange
            var protocol = new HelperSendProtocol();
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                IsAborted = true,
                TimeoutId = new CancellationTokenSource(),
                Protocol = protocol
            };
            Exception cancellationError = null;
            var resCts = new CancellationTokenSource();
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                Response = new DefaultQuasiHttpResponse
                {
                    Body = new StringBody("unbuffered"),
                    CancellationTokenSource = resCts
                }
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.False(protocol.Cancelled);
            Assert.False(instance.TimeoutId.IsCancellationRequested);
            Assert.False(request.CancellationTokenSource.IsCancellationRequested);
            Assert.True(resCts.IsCancellationRequested);
        }

        [Fact]
        public async Task TestAbort6()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                Request = request,
                ResponseBufferingEnabled = true
            };
            Exception cancellationError = null;
            var resCts = new CancellationTokenSource();
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                ResponseBufferingApplied = false,
                Response = new DefaultQuasiHttpResponse
                {
                    Body = new StringBody("ice"),
                    CancellationTokenSource = resCts
                }
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.False(resCts.IsCancellationRequested);
        }

        [Fact]
        public async Task TestAbort7()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                Request = request,
                TimeoutId = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>()
            };
            Exception cancellationError = null;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                ResponseBufferingApplied = true
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.True(instance.TimeoutId.IsCancellationRequested);

            Assert.Same(res, await instance.CancellationTcs.Task);
        }

        [Fact]
        public async Task TestAbort8()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var protocol = new HelperSendProtocol
            {
                ExpectedCancelError = new InvalidOperationException()
            };
            var instance = new SendTransferInternal
            {
                Mutex = new object(),
                Protocol = protocol,
                Request = request,
                TimeoutId = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>()
            };
            var resCts = new CancellationTokenSource();
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                Response = new DefaultQuasiHttpResponse
                {
                    CancellationTokenSource = resCts,
                    Body = new StringBody("deal")
                }
            };
            Exception cancellationError = new NotSupportedException();

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.True(request.CancellationTokenSource.IsCancellationRequested);
            Assert.True(instance.TimeoutId.IsCancellationRequested);
            Assert.True(protocol.Cancelled);
            Assert.False(resCts.IsCancellationRequested);
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                instance.CancellationTcs.Task);
        }

        class HelperSendProtocol : ISendProtocolInternal
        {
            public bool Cancelled { get; set; }
            public Exception ExpectedCancelError { get; set; }
            public Exception ExpectedSendError { get; set; }
            public ProtocolSendResultInternal ExpectedSendResult { get; set; }

            public Task Cancel()
            {
                Cancelled = true;
                if (ExpectedCancelError != null)
                {
                    return Task.FromException(ExpectedCancelError);
                }
                return Task.CompletedTask;
            }

            public Task<ProtocolSendResultInternal> Send()
            {
                if (ExpectedSendError != null)
                {
                    return Task.FromException<ProtocolSendResultInternal>(ExpectedSendError);
                }
                return Task.FromResult(ExpectedSendResult);
            }
        }
    }
}
