using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared.QuasiHttp;
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
            var protocol = new HelperSendProtocol
            {
                ExpectedSendResult = new ProtocolSendResultInternal()
            };
            var instance = new SendTransferInternal
            {
                Protocol = protocol
            };

            // act
            var actual = await instance.StartProtocol();

            // assert
            Assert.Same(protocol.ExpectedSendResult, actual);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol2()
        {
            // arrange
            var protocol = new HelperSendProtocol
            {
                ExpectedSendResult = new ProtocolSendResultInternal
                {
                    Response = new DefaultQuasiHttpResponse
                    {
                        Body = new StringBody("sth")
                    },
                    ResponseBufferingApplied = false
                }
            };
            var instance = new SendTransferInternal
            {
                Protocol = protocol
            };

            // act
            var actual = await instance.StartProtocol();

            // assert
            Assert.Same(protocol.ExpectedSendResult, actual);
            Assert.False(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol3()
        {
            // arrange
            var protocol = new HelperSendProtocol
            {
                ExpectedSendResult = new ProtocolSendResultInternal()
            };
            var instance = new SendTransferInternal
            {
                Protocol = protocol
            };
            instance.TrySetAborted();

            // act
            var actual = await instance.StartProtocol();

            // assert
            Assert.Same(protocol.ExpectedSendResult, actual);
            Assert.False(protocol.Cancelled);
        }

        [Fact]
        public async Task TestStartProtocol4()
        {
            // arrange
            var protocol = new HelperSendProtocol
            {
                ExpectedSendError = new NotImplementedException()
            };
            var instance = new SendTransferInternal
            {
                Protocol = protocol
            };

            // act and assert error
            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.StartProtocol();
            });

            // assert
            Assert.False(protocol.Cancelled);
        }

        [Fact]
        public async Task TestAbort1()
        {
            // arrange
            var requestReleaseCallCount = 0;
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCallCount++;
                }
            };
            var protocol = new HelperSendProtocol();
            var instance = new SendTransferInternal
            {
                Request = request,
                Protocol = protocol
            };
            Exception cancellationError = null;
            var responseReleaseCallCount = 0;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                ResponseBufferingApplied = true,
                Response = new ConfigurableQuasiHttpResponse
                {
                    Body = new StringBody("ice"),
                    ReleaseFunc = async () =>
                    {
                        responseReleaseCallCount++;
                    }
                }
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.Equal(1, requestReleaseCallCount);
            Assert.Equal(0, responseReleaseCallCount);
            Assert.True(protocol.Cancelled);
        }

        [Fact]
        public async Task TestAbort2()
        {
            // arrange
            var protocol = new HelperSendProtocol();
            var instance = new SendTransferInternal
            {
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
                Protocol = protocol
            };
            Exception cancellationError = null;
            instance.TrySetAborted();

            // act
            await instance.Abort(cancellationError, null);

            // assert
            Assert.False(protocol.Cancelled);
        }

        [Fact]
        public async Task TestAbort4()
        {
            // arrange
            var requestReleaseCallCount = 0;
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCallCount++;
                }
            };
            var protocol = new HelperSendProtocol();
            var instance = new SendTransferInternal
            {
                Request = request,
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>(),
                Protocol = protocol
            };
            Exception cancellationError = new InvalidOperationException();
            ProtocolSendResultInternal res = new ProtocolSendResultInternal();

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.True(protocol.Cancelled);
            Assert.Equal(1, requestReleaseCallCount);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return instance.CancellationTcs.Task;
            });
        }

        [Fact]
        public async Task TestAbort5()
        {
            // arrange
            var protocol = new HelperSendProtocol();
            var requestReleaseCount = 0;
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCount++;
                    throw new Exception("should be ignored");
                }
            };
            var instance = new SendTransferInternal
            {
                TimeoutId = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>(),
                Protocol = protocol,
                Request = request
            };
            Exception cancellationError = null;
            var responseReleaseCallCount = 0;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                Response = new ConfigurableQuasiHttpResponse
                {
                    Body = new StringBody("unbuffered"),
                    ReleaseFunc = async () =>
                    {
                        responseReleaseCallCount++;
                        throw new Exception("should not be called");
                    }
                }
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.False(protocol.Cancelled);
            Assert.True(instance.TimeoutId.IsCancellationRequested);
            Assert.Equal(1, requestReleaseCount);
            Assert.Equal(0, responseReleaseCallCount);
            Assert.Null(await instance.CancellationTcs.Task);
        }

        [Fact]
        public async Task TestAbort6()
        {
            // arrange
            var requestReleaseCount = 0;
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCount++;
                }
            };
            var protocol = new HelperSendProtocol();
            var instance = new SendTransferInternal
            {
                TimeoutId = new CancellationTokenSource(),
                Protocol = protocol
            };
            instance.TrySetAborted();
            Exception cancellationError = null;
            var responseReleaseCount = 0;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                Response = new ConfigurableQuasiHttpResponse
                {
                    Body = new StringBody("unbuffered"),
                    ReleaseFunc = async () =>
                    {
                        responseReleaseCount++;
                        throw new Exception("should be ignored");
                    }
                }
            };

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.False(protocol.Cancelled);
            Assert.False(instance.TimeoutId.IsCancellationRequested);
            Assert.Equal(0, requestReleaseCount);
            Assert.Equal(1, responseReleaseCount);
        }

        [Fact]
        public async Task TestAbort7()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest();
            var instance = new SendTransferInternal
            {
                Request = request
            };
            Exception cancellationError = null;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                ResponseBufferingApplied = false,
                Response = new DefaultQuasiHttpResponse
                {
                    Body = new StringBody("ice")
                }
            };

            // act and assert that
            // null protocol was not called.
            await instance.Abort(cancellationError, res);
        }

        [Fact]
        public async Task TestAbort8()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest();
            var instance = new SendTransferInternal
            {
                Request = request,
                TimeoutId = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>()
            };
            Exception cancellationError = null;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                ResponseBufferingApplied = true
            };

            // act and assert that
            // null protocol was not called.
            await instance.Abort(cancellationError, res);
            
            Assert.True(instance.TimeoutId.IsCancellationRequested);

            Assert.Null(await instance.CancellationTcs.Task);
        }

        [Fact]
        public async Task TestAbort9()
        {
            // arrange
            var requestReleaseCount = 0;
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCount++;
                }
            };
            var protocol = new HelperSendProtocol
            {
                ExpectedCancelError = new InvalidOperationException()
            };
            var instance = new SendTransferInternal
            {
                Protocol = protocol,
                Request = request,
                TimeoutId = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>()
            };
            var responseReleaseCount = 0;
            ProtocolSendResultInternal res = new ProtocolSendResultInternal
            {
                Response = new ConfigurableQuasiHttpResponse
                {
                    Body = new StringBody("deal"),
                    ReleaseFunc = async () =>
                    {
                        responseReleaseCount++;
                    }
                }
            };
            Exception cancellationError = new NotSupportedException();

            // act
            await instance.Abort(cancellationError, res);

            // assert
            Assert.Equal(1, requestReleaseCount);
            Assert.True(instance.TimeoutId.IsCancellationRequested);
            Assert.True(protocol.Cancelled);
            Assert.Equal(0, responseReleaseCount);
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
