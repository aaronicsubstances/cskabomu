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
        /// Gets or sets an alternative to <see cref="Transport"/>
        /// based on the <see cref="IQuasiHttpAltTransport"/> type, 
        /// for bypassing the usual connection-oriented request processing done in this class.
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
        /// <param name="sendCancellationHandle">cancellation handle received from
        /// <see cref="Send2"/> method</param>
        public void CancelSend(object sendCancellationHandle)
        {
            if (sendCancellationHandle is SendTransferInternal transfer)
            {
                var cancellationError = new QuasiHttpRequestProcessingException(
                     "send cancelled",
                    QuasiHttpRequestProcessingException.ReasonCodeCancelled);
                transfer.CancellationTcs?.TrySetException(cancellationError);
            }
        }

        /// <summary>
        /// Sends a quasi http request via quasi http transport.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the request to send</param>
        /// <param name="options">optional send options which will be merged
        /// with default send options.</param>
        /// <returns>a task whose result will be the quasi http response returned from the remote endpoint</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        public async Task<IQuasiHttpResponse> Send(object remoteEndpoint,
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
            var sendTask = await StartSend(remoteEndpoint, null,
                options, transfer);
            return await CompleteSend(transfer, sendTask);
        }

        /// <summary>
        /// Sends a quasi http request via quasi http transport and makes it posssible to cancel.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="requestFunc">a callback which receives any environment
        /// associated with the connection that may be created, or any environment
        /// that may be supplied by the <see cref="TransportBypass"/> property.
        /// Returns a promise of the request to send</param>
        /// <param name="options">optional send options which will be merged
        /// with default send options.</param>
        /// <returns>a promise of an object which contains (1) a task whose result
        /// is the quasi http response received from the remote endpoint;
        /// and (2) also contains opaque cancellation handle which can be used to cancel the request sending
        /// with <see cref="CancelSend"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="requestFunc"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        public async Task<QuasiHttpSendResponse> Send2(object remoteEndpoint,
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
            var sendTask = CompleteSend(transfer,
                await StartSend(remoteEndpoint, requestFunc, options, transfer));
            return new QuasiHttpSendResponse
            {
                ResponseTask = sendTask,
                CancellationHandle = transfer
            };
        }

        private async Task<Task<ProtocolSendResultInternal>> StartSend(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions options, SendTransferInternal transfer)
        {
            try
            {
                return await ProcessSend(remoteEndpoint, requestFunc,
                    options, transfer);
            }
            catch (Exception e)
            {
                await transfer.Abort(e, null);
                if (e is QuasiHttpRequestProcessingException)
                {
                    throw;
                }
                else
                {
                    var abortError = new QuasiHttpRequestProcessingException(
                        "encountered error during send request processing",
                        QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                        e);
                    throw abortError;
                }
            }
        }

        private static async Task<IQuasiHttpResponse> CompleteSend(
            SendTransferInternal transfer,
            Task<ProtocolSendResultInternal> sendTask)
        {
            ProtocolSendResultInternal result = null;
            IQuasiHttpResponse response;
            try
            {
                result = await sendTask;
                response = result?.Response;
            }
            catch (Exception e)
            {
                await transfer.Abort(e, result);
                if (e is QuasiHttpRequestProcessingException)
                {
                    throw;
                }
                else
                {
                    var abortError = new QuasiHttpRequestProcessingException(
                        "encountered error during send request processing",
                        QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                        e);
                    throw abortError;
                }
            }
            return response;
        }

        private async Task<Task<ProtocolSendResultInternal>> ProcessSend(
            object remoteEndpoint,
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
                QuasiHttpUtils.ConnectivityParamFireAndForget);
            var defaultForEnsureNonNullResponse = true;
            if (connectivityParamFireAndForget == true)
            {
                defaultForEnsureNonNullResponse = false;
            }
            mergedSendOptions.EnsureNonNullResponse = ProtocolUtilsInternal.DetermineEffectiveBooleanOption(
                options?.EnsureNonNullResponse,
                defaultSendOptions?.EnsureNonNullResponse,
                defaultForEnsureNonNullResponse);

            if (!skipSettingTimeouts)
            {
                transfer.TimeoutId = ProtocolUtilsInternal.CreateCancellableTimeoutTask<ProtocolSendResultInternal>(
                    mergedSendOptions.TimeoutMillis, "send timeout");
            }

            Task<ProtocolSendResultInternal> workTask;
            if (transportBypass != null)
            {
                workTask = await DirectSend(remoteEndpoint, requestFunc,
                    mergedSendOptions, transfer, transportBypass);
            }
            else
            {
                workTask = await AllocateConnectionAndSend(remoteEndpoint, requestFunc,
                    mergedSendOptions, transfer, Transport);
            }
            return ProtocolUtilsInternal.CompleteRequestProcessing(workTask,
                transfer.TimeoutId.Task, transfer.CancellationTcs?.Task);
        }

        private async static Task<Task<ProtocolSendResultInternal>> DirectSend(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions mergedSendOptions,
            SendTransferInternal transfer,
            IQuasiHttpAltTransport transportBypass)
        {
            QuasiHttpSendResponse response;
            if (requestFunc != null)
            {
                response = await transportBypass.ProcessSendRequest2(remoteEndpoint,
                    requestFunc, mergedSendOptions);
            }
            else
            {
                response = await transportBypass.ProcessSendRequest(remoteEndpoint,
                    transfer.Request, mergedSendOptions);
            }
            transfer.Protocol = new AltSendProtocolInternal
            {
                TransportBypass = transportBypass,
                ResponseBufferingEnabled = mergedSendOptions.ResponseBufferingEnabled.Value,
                ResponseBodyBufferingSizeLimit = mergedSendOptions.ResponseBodyBufferingSizeLimit,
                ResponseTask = response.ResponseTask,
                SendCancellationHandle = response.CancellationHandle,
                EnsureNonNullResponse = mergedSendOptions.EnsureNonNullResponse.Value,
            };
            return transfer.StartProtocol();
        }

        private static async Task<Task<ProtocolSendResultInternal>> AllocateConnectionAndSend(
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
            return transfer.StartProtocol();
        }
    }
}
