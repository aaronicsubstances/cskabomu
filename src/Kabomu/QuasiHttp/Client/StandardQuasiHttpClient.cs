using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
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
        private readonly Random _randGen = new Random();
        private readonly Func<object, Exception, IQuasiHttpResponse, Task> AbortTransferCallback;
        private readonly Func<object, IQuasiHttpResponse, Task> AbortTransferCallback2;

        /// <summary>
        /// Creates a new instance of the <see cref="StandardQuasiHttpClient"/> class with defaults provided
        /// for the <see cref="MutexApi"/> and <see cref="TimerApi"/> properties.
        /// </summary>
        public StandardQuasiHttpClient()
        {
            MutexApi = new LockBasedMutexApi();
            TimerApi = new DefaultTimerApi();
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
        /// Gets or sets a value from 0-1 for deciding on whether to wrap a request or
        /// a response with proxy objects when using <see cref="IQuasiHttpAltTransport"/>
        /// implementations.
        /// <para></para>
        /// E.g. a value of 0 means never use wrap; a value of 0.1 means almost never
        /// wrap; a value of 0.9 means almost always wrap; and a value of 1 means always wrap a request (or
        /// response) object with a proxy object.
        /// </summary>
        /// <remarks>
        /// The purpose of this property is to help prevent end users of IQuasiHttpAltTransport 
        /// implementations from presuming use of a particular request or request body class, when
        /// using them with this class.
        /// <para></para>
        /// By default, the value of this property is zero, meaning that wrapping step is omitted for
        /// request and response bodies.
        /// <para></para>
        /// NB: negative values are treated as equivalent to zero; and values larger than 1 are treated as
        /// equivalent to 1.
        /// </remarks>
        public double TransportBypassWrappingProbability { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets mutex api used to guard multithreaded 
        /// access to connection allocation operations of this class.
        /// </summary>
        /// <remarks> 
        /// An ordinary lock object is the initial value for this property, and so there is no need to modify
        /// this property except for advanced scenarios.
        /// </remarks>
        public IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Gets or sets timer api used to generate timeouts in this class.
        /// </summary>
        /// <remarks> 
        /// An instance of <see cref="DefaultTimerApi"/> class is the initial value for this property,
        /// and so there is no need to modify this property except for advanced scenarios.
        /// </remarks>
        public ITimerApi TimerApi { get; set; }

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
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint
                },
                Request = request,
                SendOptions = options
            };
            return ProcessSend(transfer, false);
        }

        /// <summary>
        /// Sends a quasi http request via quasi http transport and makes it posssible to cancel.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the request to send</param>
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
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var transfer = new SendTransferInternal
            {
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint
                },
                Request = request,
                SendOptions = options
            };
            var sendTask = ProcessSend(transfer, true);
            return (sendTask, transfer);
        }

        private async Task<IQuasiHttpResponse> ProcessSend(SendTransferInternal transfer,
            bool setUpCancellation)
        {
            Task<IQuasiHttpResponse> workTask;
            using (await MutexApi.Synchronize())
            {
                // set up transfer dependencies
                transfer.MutexApi = MutexApi;
                transfer.TimerApi = TimerApi;

                // NB: negative value is allowed for timeout, which indicates infinite timeout.
                transfer.TimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    transfer.SendOptions?.TimeoutMillis,
                    DefaultSendOptions?.TimeoutMillis,
                    0);
                transfer.SetSendTimeout();

                if (transfer.TimeoutId != null || setUpCancellation)
                {
                    transfer.CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                }

                transfer.ConnectivityParams.ExtraParams = ProtocolUtilsInternal.DetermineEffectiveOptions(
                    transfer.SendOptions?.ExtraConnectivityParams,
                    DefaultSendOptions?.ExtraConnectivityParams);

                transfer.ResponseBufferingEnabled = ProtocolUtilsInternal.DetermineEffectiveBooleanOption(
                    transfer.SendOptions?.ResponseBufferingEnabled,
                    DefaultSendOptions?.ResponseBufferingEnabled,
                    true);

                transfer.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    transfer.SendOptions?.MaxChunkSize,
                    DefaultSendOptions?.MaxChunkSize,
                    TransportUtils.DefaultMaxChunkSize);

                transfer.ResponseBodyBufferingSizeLimit = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    transfer.SendOptions?.ResponseBodyBufferingSizeLimit,
                    DefaultSendOptions?.ResponseBodyBufferingSizeLimit,
                    TransportUtils.DefaultResponseBodyBufferingSizeLimit);

                transfer.RequestWrappingEnabled = _randGen.NextDouble() < TransportBypassWrappingProbability;
                transfer.ResponseWrappingEnabled = _randGen.NextDouble() < TransportBypassWrappingProbability;

                if (TransportBypass != null)
                {
                    workTask = ProcessSendRequestDirectly(transfer);
                }
                else
                {
                    workTask = AllocateConnectionAndSend(transfer);
                }
            }

            try
            {
                if (transfer.CancellationTcs != null)
                {
                    await await Task.WhenAny(transfer.CancellationTcs.Task, workTask);
                }
                else
                {
                    return await workTask;
                }
            }
            catch (Exception e)
            {
                // let call to abort transfer determine whether exception is significant.
                QuasiHttpRequestProcessingException abortError;
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
                await transfer.Abort(abortError, null);
                if (transfer.CancellationTcs == null)
                {
                    throw abortError;
                }
            }
            
            // by awaiting again for transfer cancellation, any significant error will bubble up, and
            // any insignificant error will be swallowed.
            return await transfer.CancellationTcs.Task;
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestDirectly(SendTransferInternal transfer)
        {
            var protocol = new AltSendProtocolInternal
            {
                TransportBypass = TransportBypass,
                ConnectivityParams = transfer.ConnectivityParams,
                ResponseBufferingEnabled = transfer.ResponseBufferingEnabled,
                ResponseBodyBufferingSizeLimit = transfer.ResponseBodyBufferingSizeLimit,
                MaxChunkSize = transfer.MaxChunkSize,
                RequestWrappingEnabled = transfer.RequestWrappingEnabled,
                ResponseWrappingEnabled = transfer.ResponseWrappingEnabled
            };
            transfer.Protocol = protocol;
            var res = await protocol.Send(transfer.Request);
            await transfer.Abort(null, res);
            return res.Response;
        }

        private async Task<IQuasiHttpResponse> AllocateConnectionAndSend(SendTransferInternal transfer)
        {
            var transport = Transport;
            if (transport == null)
            {
                throw new MissingDependencyException("transport");
            }

            var connectionResponse = await transport.AllocateConnection(transfer.ConnectivityParams);

            Task releaseTask = null;
            Task<ProtocolSendResult> sendTask = null;
            using (await MutexApi.Synchronize())
            {
                if (connectionResponse?.Connection == null)
                {
                    throw new ExpectationViolationException("no connection");
                }

                if (transfer.IsAborted)
                {
                    // Oops...connection established took so long, or a cancellation happened.
                    // just release the connection.
                    releaseTask = transport.ReleaseConnection(connectionResponse.Connection);
                }
                else
                {
                    var protocol = new DefaultSendProtocolInternal
                    {
                        Transport = transport,
                        Connection = connectionResponse.Connection,
                        ResponseBufferingEnabled = transfer.ResponseBufferingEnabled,
                        ResponseBodyBufferingSizeLimit = transfer.ResponseBodyBufferingSizeLimit,
                        MaxChunkSize = transfer.MaxChunkSize,
                    };
                    transfer.Protocol = protocol;

                    sendTask = protocol.Send(transfer.Request);
                }
            }

            if (releaseTask != null)
            {
                await releaseTask;
            }

            if (sendTask != null)
            {
                var res = await sendTask;
                await transfer.Abort(null, res);
                return res.Response;
            }

            return null;
        }
    }
}
