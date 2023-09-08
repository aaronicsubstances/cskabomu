using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Kabomu.Tests.Shared")]
[assembly: InternalsVisibleTo("Kabomu.Tests")]
[assembly: InternalsVisibleTo("Kabomu.IntegrationTests")]

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
        /// associated with the connection that may be created.
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
                throw new MissingDependencyException("transport");
            }

            var connection = await transport.AllocateConnection(
                remoteEndpoint, sendOptions);
            if (connection == null)
            {
                throw new QuasiHttpRequestProcessingException("no connection");
            }

            if (request == null)
            {
                request = await requestFunc.Invoke(connection.Environment);
                if (request == null)
                {
                    throw new QuasiHttpRequestProcessingException("no request");
                }
            }

            try
            {
                var response = await ProcessSend(request, transport,
                    connection);
                return response;
            }
            catch (Exception e)
            {
                await Abort(transport, connection, true);
                if (e is QuasiHttpRequestProcessingException)
                {
                    throw;
                }
                var abortError = new QuasiHttpRequestProcessingException(
                    "encountered error during send request processing",
                    QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                    e);
                throw abortError;
            }
        }

        internal static async Task<IQuasiHttpResponse> ProcessSend(
            IQuasiHttpRequest request,
            IQuasiHttpClientTransport transport,
            IQuasiHttpConnection connection)
        {
            if (transport == null)
            {
                throw new MissingDependencyException("client transport");
            }
            if (request == null)
            {
                throw new ExpectationViolationException("request");
            }

            // send entire request first before
            // receiving of response.
            if (QuasiHttpProtocolUtils.GetEnvVarAsBoolean(request.Environment,
                QuasiHttpProtocolUtils.EnvKeySkipSending) != true)
            {
                var encodedRequest = new DefaultEncodedQuasiHttpEntity();
                encodedRequest.Headers = QuasiHttpProtocolUtils.EncodeRequestHeaders(request,
                    connection.ProcessingOptions?.MaxHeadersSize);
                encodedRequest.Body = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                    request.ContentLength, request.Body, request.Environment);
                await transport.Write(connection, false, encodedRequest);
            }

            var encodedResponse = await transport.Read(connection, true);
            if (encodedResponse?.Headers == null)
            {
                throw new QuasiHttpRequestProcessingException("no response");
            }

            Func<Task> releaseFunc = () =>
            {
                return transport.ReleaseConnection(connection, false);
            };
            var response = new DefaultQuasiHttpResponse
            {
                Disposer = releaseFunc
            };
            QuasiHttpProtocolUtils.DecodeResponseHeaders(
                encodedResponse.Headers, response);
            response.Body = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                response.ContentLength, encodedResponse.Body,
                connection.Environment);
            // apply response buffering, but only if response body exists.
            bool responseStreamingEnabled = response.Body != null;
            if (responseStreamingEnabled &&
                connection.ProcessingOptions?.ResponseBufferingEnabled != false)
            {
                response.Body = await transport.ApplyResponseBuffering(connection,
                    response.Body);
                responseStreamingEnabled = false;
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
