using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Kabomu.Tests")]

namespace Kabomu
{
    /// <summary>
    /// The standard implementation of the client side of the quasi http protocol defined by the Kabomu library.
    /// </summary>
    /// <remarks>
    /// This class provides the client facing side of networking for end users. It is the complement to the 
    /// <see cref="StandardQuasiHttpServer"/> class for supporting the semantics of HTTP client libraries
    /// whiles enabling underlying transport options beyond TCP.
    /// <para></para>
    /// Therefore this class can be seen as the equivalent of an HTTP client that extends underlying transport beyond TCP
    /// to IPC mechanisms.
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
        /// Gets or sets the underlying transport (TCP or IPC) by which connections
        /// will be allocated for sending requests and receiving responses.
        /// </summary>
        public virtual IQuasiHttpClientTransport Transport { get; set; }

        /// <summary>
        /// Sends a quasi http request via quasi http transport.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the request to send</param>
        /// <param name="options">optional send options</param>
        /// <returns>a task whose result will be the quasi http response returned from the remote endpoint</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        public async Task<IQuasiHttpResponse> Send(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpProcessingOptions options = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await SendInternal(remoteEndpoint, request, null, options);
        }

        /// <summary>
        /// Sends a quasi http request via quasi http transport and makes it posssible to
        /// receive connection allocation information before creating request.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="requestFunc">a callback which receives any environment
        /// associated with the connection that is created.
        /// Returns a promise of the request to send</param>
        /// <param name="options">optional send options</param>
        /// <returns>a task whose result will be the quasi http response returned from the remote endpoint.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="requestFunc"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property is null.</exception>
        public async Task<IQuasiHttpResponse> Send2(object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpProcessingOptions options = null)
        {
            if (requestFunc == null)
            {
                throw new ArgumentNullException(nameof(requestFunc));
            }
            return await SendInternal(remoteEndpoint, null, requestFunc,
                options);
        }

        private async Task<IQuasiHttpResponse> SendInternal(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpProcessingOptions sendOptions)
        {
            // access fields for use per request call, in order to cooperate with
            // any implementation of field accessors which supports
            // concurrent modifications.
            var transport = Transport;

            if (transport == null)
            {
                throw new MissingDependencyException("client transport");
            }

            var connectionAllocationResponse = await transport.AllocateConnection(
                remoteEndpoint, sendOptions);
            var connection = connectionAllocationResponse?.Connection;
            if (connection == null)
            {
                throw new QuasiHttpException("no connection");
            }
            try
            {
                var responseTask = ProcessSend(request, requestFunc,
                    transport, connection, connectionAllocationResponse);
                if (connection.TimeoutTask != null)
                {
                    var timeoutTask = ProtocolUtilsInternal.WrapTimeoutTask(
                        connection.TimeoutTask, "send timeout");
                    await await Task.WhenAny(responseTask, timeoutTask);
                }
                return await responseTask;
            }
            catch (Exception e)
            {
                await Abort(transport, connection, true);
                if (e is QuasiHttpException)
                {
                    throw;
                }
                var abortError = new QuasiHttpException(
                    "encountered error during send request processing",
                    QuasiHttpException.ReasonCodeGeneral,
                    e);
                throw abortError;
            }
        }

        internal static async Task<IQuasiHttpResponse> ProcessSend(
            IQuasiHttpRequest request,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpClientTransport transport,
            IQuasiHttpConnection connection,
            IConnectionAllocationResponse connectionAllocationResponse)
        {
            var ongoingConnectionTask = connectionAllocationResponse.ConnectTask;
            if (ongoingConnectionTask != null)
            {
                // wait for connection to be completely established.
                await ongoingConnectionTask;
            }

            if (request == null)
            {
                request = await requestFunc.Invoke(connection.Environment);
                if (request == null)
                {
                    throw new QuasiHttpException("no request");
                }
            }

            // send entire request first before
            // receiving of response.
            var altTransport = transport as IQuasiHttpAltTransport;
            var requestSerializer = altTransport?.RequestSerializer;
            bool requestSerialized = false;
            if (requestSerializer != null)
            {
                requestSerialized = await requestSerializer(connection, request);
            }
            if (!requestSerialized)
            {
                await ProtocolUtilsInternal.WriteEntityToTransport(
                    false, request, transport.GetWritableStream(connection),
                    connection);
            }

            IQuasiHttpResponse response = null;
            var responseDeserializer = altTransport?.ResponseDeserializer;
            var responseStreamingEnabled = false;
            if (responseDeserializer != null)
            {
                // let response deserializer take control of response buffering.
                response = await responseDeserializer(connection);
            }
            if (response == null)
            {
                response = (IQuasiHttpResponse)await ProtocolUtilsInternal.ReadEntityFromTransport(
                    true, transport.GetReadableStream(connection), connection);
                var applyResponseBuffering = connection.ProcessingOptions?.ResponseBufferingEnabled != false;
                if (applyResponseBuffering)
                {
                    responseStreamingEnabled = false;
                    response.Body = await ProtocolUtilsInternal.BufferResponseBody(
                        response.Body, connection);
                }
                else
                {
                    responseStreamingEnabled = response.Body != null;
                }
            }
            if (responseStreamingEnabled)
            {
                response.Disposer = () =>
                {
                    return transport.ReleaseConnection(connection, false);
                };
            }
            await Abort(transport, connection, false, responseStreamingEnabled);
            return response;
        }

        internal async static Task Abort(IQuasiHttpClientTransport transport,
            IQuasiHttpConnection connection,
            bool errorOccured, bool responseStreamingEnabled = false)
        {
            if (errorOccured)
            {
                try
                {
                    // don't wait.
                    _ = transport.ReleaseConnection(connection, false);
                }
                catch (Exception) { } // ignore
            }
            else
            {
                await transport.ReleaseConnection(connection,
                    responseStreamingEnabled);
            }
        }
    }
}
