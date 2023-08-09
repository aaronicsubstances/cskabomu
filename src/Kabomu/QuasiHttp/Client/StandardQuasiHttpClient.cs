using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Kabomu.Tests.Shared")]
[assembly: InternalsVisibleTo("Kabomu.Tests")]
[assembly: InternalsVisibleTo("Kabomu.IntegrationTests")]

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
    public class StandardQuasiHttpClient
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public StandardQuasiHttpClient()
        {
        }

        /// <summary>
        /// Gets or sets the default options used to send requests.
        /// </summary>
        public virtual IQuasiHttpSendOptions DefaultSendOptions { get; set; }

        /// <summary>
        /// Gets or sets the underlying transport (TCP or IPC) by which connections
        /// will be allocated for sending requests and receiving responses.
        /// </summary>
        public virtual IQuasiHttpClientTransport Transport { get; set; }

        /// <summary>
        /// Gets or sets an instance of the <see cref="IQuasiHttpAltTransport"/> type for bypassing the usual
        /// connection-oriented request processing done in this class.
        /// </summary>
        /// <remarks>
        /// By this property, any network can be used to send quasi http requests since it
        /// effectively receives full responsibility for sending the request.
        /// </remarks>
        public virtual IQuasiHttpAltTransport TransportBypass { get; set; }

        /// <summary>
        /// Can be used by transports which want to take charge of timeout
        /// settings, to avoid the need for an instance of this class to
        /// skip setting timeouts.
        /// </summary>
        public virtual bool IgnoreTimeoutSettings { get; set; }

        /// <summary>
        /// Cancels a send request if it is still ongoing. Invalid cancellation handles are simply ignored.
        /// </summary>
        /// <param name="sendCancellationHandle">cancellation handle received from <see cref="Send"/></param>
        public void CancelSend(object sendCancellationHandle)
        {
            if (sendCancellationHandle is SendTransferInternal transfer)
            {
                var cancellationError = new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeCancelled, "send cancelled");
                transfer.CancellationTcs?.TrySetException(cancellationError);
            }
        }

        /// <summary>
        /// Simply sends a quasi http request via quasi http transport.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the request to send</param>
        /// <param name="options">send options which override default send options</param>
        /// <returns>a task whose result will be the quasi http response returned from the remote endpoint</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        public Task<IQuasiHttpResponse> Send(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var transfer = new SendTransferInternal
            {
                Request = request
            };
            return Send(remoteEndpoint, null, options, transfer);
        }

        /// <summary>
        /// Sends a quasi http request via quasi http transport and makes it posssible to cancel.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="requestFunc">a callback which receives any environment
        /// associated with the connection that may be created, or any environment
        /// that may be supplied by the <see cref="TransportBypass"/> property.
        /// Returns a promise of the request to send</param>
        /// <param name="options">send options which override default send options</param>
        /// <returns>pair of handles: first is a task which can be used to await quasi http response from the remote endpoint;
        /// second is an opaque cancellation handle which can be used to cancel the request sending.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="requestFunc"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        public (Task<IQuasiHttpResponse>, object) Send(object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions options)
        {
            if (requestFunc == null)
            {
                throw new ArgumentNullException(nameof(requestFunc));
            }

            var transfer = new SendTransferInternal
            {
                CancellationTcs = new TaskCompletionSource<ProtocolSendResultInternal>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            var sendTask = Send(remoteEndpoint, requestFunc,
                options, transfer);
            return (sendTask, transfer);
        }

        private async Task<IQuasiHttpResponse> Send(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions options, SendTransferInternal transfer)
        {
            ProtocolSendResultInternal res = null;
            IQuasiHttpResponse response = null;
            try
            {
                res = await ProcessSend(remoteEndpoint, requestFunc,
                    options, transfer);
                response = res?.Response;
            }
            catch (Exception e)
            {
                await transfer.Abort(e, res);
                if (e is QuasiHttpRequestProcessingException)
                {
                    throw;
                }
                else
                {
                    var abortError = new QuasiHttpRequestProcessingException(
                        QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                        "encountered error during send request processing", e);
                    throw abortError;
                }
            }
            return response;
        }

        private async Task<ProtocolSendResultInternal> ProcessSend(object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions options, SendTransferInternal transfer)
        {
            // access fields for use per request call, in order to cooperate with
            // any implementation of field accessors which supports
            // concurrent modifications.
            var defaultSendOptions = DefaultSendOptions;
            var transportBypass = TransportBypass;
            var skipSettingTimeouts = IgnoreTimeoutSettings;

            // NB: negative value is allowed for timeout, which indicates infinite timeout.
            var mergedSendOptions = new DefaultQuasiHttpSendOptions();
            mergedSendOptions.TimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                options?.TimeoutMillis,
                defaultSendOptions?.TimeoutMillis,
                0);
            Task<ProtocolSendResultInternal> timeoutTask = null;
            if (!skipSettingTimeouts)
            {
                (timeoutTask, transfer.TimeoutId) = ProtocolUtilsInternal.SetTimeout<ProtocolSendResultInternal>(
                    mergedSendOptions.TimeoutMillis, "send timeout");
            }

            mergedSendOptions.ExtraConnectivityParams = ProtocolUtilsInternal.DetermineEffectiveOptions(
                options?.ExtraConnectivityParams,
                defaultSendOptions?.ExtraConnectivityParams);

            mergedSendOptions.ResponseBufferingEnabled = ProtocolUtilsInternal.DetermineEffectiveBooleanOption(
                options?.ResponseBufferingEnabled,
                defaultSendOptions?.ResponseBufferingEnabled,
                true);

            mergedSendOptions.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                options?.MaxChunkSize,
                defaultSendOptions?.MaxChunkSize,
                0);

            mergedSendOptions.ResponseBodyBufferingSizeLimit = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                options?.ResponseBodyBufferingSizeLimit,
                defaultSendOptions?.ResponseBodyBufferingSizeLimit,
                0);

            var connectivityParamFireAndForget = ProtocolUtilsInternal.GetEnvVarAsBoolean(
                mergedSendOptions.ExtraConnectivityParams,
                TransportUtils.ConnectivityParamFireAndForget);
            var defaultForEnsureNonNullResponse = true;
            if (connectivityParamFireAndForget == true)
            {
                defaultForEnsureNonNullResponse = false;
            }
            mergedSendOptions.EnsureNonNullResponse = ProtocolUtilsInternal.DetermineEffectiveBooleanOption(
                options?.EnsureNonNullResponse,
                defaultSendOptions?.EnsureNonNullResponse,
                defaultForEnsureNonNullResponse);

            Task<ProtocolSendResultInternal> workTask;
            if (transportBypass != null)
            {
                workTask = DirectSend(remoteEndpoint, requestFunc,
                    mergedSendOptions, transfer, transportBypass);
            }
            else
            {
                workTask = AllocateConnectionAndSend(remoteEndpoint, requestFunc,
                    mergedSendOptions, transfer, Transport);
            }
            return await ProtocolUtilsInternal.CompleteRequestProcessing(workTask,
                timeoutTask, transfer.CancellationTcs?.Task);
        }

        private static async Task<ProtocolSendResultInternal> DirectSend(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions mergedSendOptions,
            SendTransferInternal transfer,
            IQuasiHttpAltTransport transportBypass)
        {
            Task<IQuasiHttpResponse> responseTask;
            object sendCancellationHandle;
            if (requestFunc != null)
            {
                (responseTask, sendCancellationHandle) =
                    transportBypass.ProcessSendRequest(remoteEndpoint,
                        requestFunc, mergedSendOptions);
            }
            else
            {
                (responseTask, sendCancellationHandle) =
                    transportBypass.ProcessSendRequest(remoteEndpoint,
                        transfer.Request, mergedSendOptions);
            }
            transfer.Protocol = new AltSendProtocolInternal
            {
                TransportBypass = transportBypass,
                ResponseBufferingEnabled = mergedSendOptions.ResponseBufferingEnabled.Value,
                ResponseBodyBufferingSizeLimit = mergedSendOptions.ResponseBodyBufferingSizeLimit,
                ResponseTask = responseTask,
                SendCancellationHandle = sendCancellationHandle,
                EnsureNonNullResponse = mergedSendOptions.EnsureNonNullResponse.Value,
            };
            return await transfer.StartProtocol();
        }

        private static async Task<ProtocolSendResultInternal> AllocateConnectionAndSend(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions mergedSendOptions,
            SendTransferInternal transfer,
            IQuasiHttpClientTransport transport)
        {
            if (transport == null)
            {
                throw new MissingDependencyException("transport");
            }

            var connectionResponse = await transport.AllocateConnection(
                remoteEndpoint, mergedSendOptions);
            var connection = connectionResponse?.Connection;
            if (connection == null)
            {
                throw new QuasiHttpRequestProcessingException("no connection");
            }

            if (transfer.Request == null)
            {
                var request = await requestFunc.Invoke(connectionResponse.Environment);
                if (request == null)
                {
                    throw new QuasiHttpRequestProcessingException("no request");
                }
                transfer.Request = request;
            }

            transfer.Protocol = new DefaultSendProtocolInternal
            {
                Request = transfer.Request,
                Transport = transport,
                Connection = connection,
                ResponseBufferingEnabled = mergedSendOptions.ResponseBufferingEnabled.Value,
                ResponseBodyBufferingSizeLimit = mergedSendOptions.ResponseBodyBufferingSizeLimit,
                MaxChunkSize = mergedSendOptions.MaxChunkSize,
                EnsureNonNullResponse = mergedSendOptions.EnsureNonNullResponse.Value,
            };
            return await transfer.StartProtocol();
        }
    }
}
