using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Kabomu.Tests")]

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// The standard implementation of the client side of the quasi http protocol defined by the Kabomu library.
    /// </summary>
    /// <remarks>
    /// This class provides the client facing side of networking for end users. It is the complement to the 
    /// <see cref="Server.StandardQuasiHttpServer"/> class for supporting the semantics of HTTP client libraries
    /// whiles enabling underlying transport options beyond TCP.
    /// <para></para>
    /// Therefore this class can be seen as the equivalent of an HTTP client that extends underlying transport beyond TCP
    /// to IPC mechanisms and even interested connectionless transports as well.
    /// </remarks>
    public class StandardQuasiHttpClient : IQuasiHttpClient
    {
        private readonly object _mutex = new object();

        /// <summary>
        /// Creates a new instance of the <see cref="StandardQuasiHttpClient"/> class with defaults provided
        /// for the <see cref="MutexApi"/> and <see cref="TimerApi"/> properties.
        /// </summary>
        public StandardQuasiHttpClient()
        {
            DefaultProtocolFactory = transfer =>
            {
                return new DefaultSendProtocolInternal
                {
                    Request = transfer.Request,
                    Transport = Transport,
                    Connection = transfer.Connection,
                    ResponseBufferingEnabled = transfer.ResponseBufferingEnabled,
                    ResponseBodyBufferingSizeLimit = transfer.ResponseBodyBufferingSizeLimit,
                    MaxChunkSize = transfer.MaxChunkSize,
                };
            };
            AltProtocolFactory = transfer =>
            {
                return new AltSendProtocolInternal
                {
                    Request = transfer.Request,
                    RequestFunc = transfer.RequestFunc,
                    TransportBypass = TransportBypass,
                    ConnectivityParams = transfer.ConnectivityParams,
                    ResponseBufferingEnabled = transfer.ResponseBufferingEnabled,
                    ResponseBodyBufferingSizeLimit = transfer.ResponseBodyBufferingSizeLimit
                };
            };
        }

        /// <summary>
        /// Gets or sets the default options used to send requests.
        /// </summary>
        public IQuasiHttpSendOptions DefaultSendOptions { get; set; }

        /// <summary>
        /// Gets or sets the underlying transport (TCP or IPC) by which connections
        /// will be allocated for sending requests and receiving responses.
        /// </summary>
        public IQuasiHttpClientTransport Transport { get; set; }

        /// <summary>
        /// Gets or sets an instance of the <see cref="IQuasiHttpAltTransport"/> type for bypassing the usual
        /// connection-oriented request processing done in this class.
        /// </summary>
        /// <remarks>
        /// By this property, any network can be used to send quasi http requests since it
        /// effectively receives full responsibility for sending the request.
        /// </remarks>
        public IQuasiHttpAltTransport TransportBypass { get; set; }

        /// <summary>
        /// Exposed for testing.
        /// </summary>
        internal Func<SendTransferInternal, ISendProtocolInternal> DefaultProtocolFactory { get; set; }

        /// <summary>
        /// Exposed for testing.
        /// </summary>
        internal Func<SendTransferInternal, ISendProtocolInternal> AltProtocolFactory { get; set; }

        /// <summary>
        /// Cancels a send request if it is still ongoing. Invalid cancellation handles are simply ignored.
        /// </summary>
        /// <param name="sendCancellationHandle">cancellation handle received from <see cref="Send2"/></param>
        public void CancelSend(object sendCancellationHandle)
        {
            if (sendCancellationHandle is SendTransferInternal transfer)
            {
                var cancellationError = new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeCancelled, "send cancelled");
                // don't wait
                _ = transfer.Abort(cancellationError, null);
            }
        }

        /// <summary>
        /// Simply sends a quasi http request via quasi http transport.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the request to send</param>
        /// <param name="options">send options which override default send options and response
        /// streaming probability.</param>
        /// <returns>a task whose result will be the quasi http response returned from the remote endpoint</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="TimerApi"/>
        /// property is null at a point where timer functionality is needed.</exception>
        public Task<IQuasiHttpResponse> Send(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var transfer = new SendTransferInternal
            {
                Mutex = _mutex
            };
            return ProcessSendX(remoteEndpoint, request, null, options, transfer);
        }

        /// <summary>
        /// Sends a quasi http request via quasi http transport and makes it posssible to cancel.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="requestFunc">a callback which receives any environment
        /// associated with the connection that may be created, and returns a promise of
        /// the request to send</param>
        /// <param name="options">send options which override default send options and response
        /// streaming probability.</param>
        /// <returns>pair of handles: first is a task which can be used to await quasi http response from the remote endpoint;
        /// second is an opaque cancellation handle which can be used to cancel the request sending.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="TimerApi"/>
        /// property is null at a point where timer functionality is needed.</exception>
        public (Task<IQuasiHttpResponse>, object) Send2(object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions options)
        {
            if (requestFunc == null)
            {
                throw new ArgumentNullException(nameof(requestFunc));
            }

            var transfer = new SendTransferInternal
            {
                Mutex = _mutex,
                CancellationTcs = new TaskCompletionSource<ProtocolSendResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            var sendTask = ProcessSendX(remoteEndpoint, null, requestFunc,
                options, transfer);
            return (sendTask, transfer);
        }

        private async Task<IQuasiHttpResponse> ProcessSendX(
            object remoteEndpoint, IQuasiHttpRequest request,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions options, SendTransferInternal transfer)
        {
            ProtocolSendResult res = null;
            IQuasiHttpResponse response = null;
            QuasiHttpRequestProcessingException abortError = null;
            try
            {
                res = await ProcessSend(remoteEndpoint, request, requestFunc,
                    options, transfer);
                response = res?.Response;
                if (response == null)
                {
                    throw new ExpectationViolationException("expected non-null response");
                }
            }
            catch (Exception e)
            {
                if (e is QuasiHttpRequestProcessingException quasiHttpError)
                {
                    abortError = quasiHttpError;
                }
                else
                {
                    abortError = new QuasiHttpRequestProcessingException(
                        QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                        "encountered error during send request processing", e);
                }
            }
            await transfer.Abort(abortError, res);
            if (abortError != null)
            {
                throw abortError;
            }
            return response;
        }

        private async Task<ProtocolSendResult> ProcessSend(object remoteEndpoint,
            IQuasiHttpRequest request,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions options, SendTransferInternal transfer)
        {
            Task<ProtocolSendResult> workTask;
            Task<ProtocolSendResult> timeoutTask;
            lock (_mutex)
            {
                // NB: negative value is allowed for timeout, which indicates infinite timeout.
                var timeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    options?.TimeoutMillis,
                    DefaultSendOptions?.TimeoutMillis,
                    0);
                (timeoutTask, transfer.TimeoutId) = ProtocolUtilsInternal.SetTimeout<ProtocolSendResult>(timeoutMillis);

                var effectiveConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint
                };
                effectiveConnectivityParams.ExtraParams = ProtocolUtilsInternal.DetermineEffectiveOptions(
                    options?.ExtraConnectivityParams,
                    DefaultSendOptions?.ExtraConnectivityParams);
                transfer.ConnectivityParams = effectiveConnectivityParams;

                transfer.ResponseBufferingEnabled = ProtocolUtilsInternal.DetermineEffectiveBooleanOption(
                    options?.ResponseBufferingEnabled,
                    DefaultSendOptions?.ResponseBufferingEnabled,
                    true);

                transfer.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    options?.MaxChunkSize,
                    DefaultSendOptions?.MaxChunkSize,
                    0);

                transfer.ResponseBodyBufferingSizeLimit = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    options?.ResponseBodyBufferingSizeLimit,
                    DefaultSendOptions?.ResponseBodyBufferingSizeLimit,
                    0);

                transfer.Request = request;

                if (TransportBypass != null)
                {
                    transfer.RequestFunc = requestFunc;
                    workTask = transfer.StartProtocol(AltProtocolFactory);
                }
                else
                {
                    workTask = AllocateConnectionAndSend(transfer, requestFunc);
                }
            }
            return await ProtocolUtilsInternal.CompleteRequestProcessing(workTask,
                timeoutTask, transfer.CancellationTcs.Task);
        }

        private async Task<ProtocolSendResult> AllocateConnectionAndSend(SendTransferInternal transfer,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc)
        {
            var transport = Transport;
            if (transport == null)
            {
                throw new MissingDependencyException("transport");
            }

            var connectionResponse = await transport.AllocateConnection(transfer.ConnectivityParams);

            Task<ProtocolSendResult> workTask = null;
            lock (_mutex)
            {
                if (connectionResponse?.Connection == null)
                {
                    throw new QuasiHttpRequestProcessingException("no connection");
                }

                transfer.Connection = connectionResponse.Connection;
                if (requestFunc != null)
                {
                    workTask = transfer.StartProtocol(DefaultProtocolFactory);
                }
            }

            if (workTask == null)
            {
                var request = await requestFunc.Invoke(connectionResponse.Environment);
                if (request == null)
                {
                    throw new QuasiHttpRequestProcessingException("no request");
                }
                transfer.Request = request;
                lock (_mutex)
                {
                    workTask = transfer.StartProtocol(DefaultProtocolFactory);
                }
            }
            return await workTask;
        }
    }
}
