﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// The standard implementation of the server side of the quasi http protocol defined by the Kabomu library.
    /// </summary>
    /// <remarks>
    /// This class provides the server facing side of networking for end users. It is the complement to the 
    /// <see cref="StandardQuasiHttpClient"/> class for providing HTTP semantics for web application frameworks
    /// whiles enabling underlying transport options beyond TCP.
    /// <para></para>
    /// Therefore this class can be seen as the equivalent of an HTTP server in which the underlying transport of
    /// choice extends beyond TCP to include IPC mechanisms.
    /// </remarks>
    public class StandardQuasiHttpServer
    {
        private static readonly QuasiHttpHeadersCodec HeadersCodec =
            new QuasiHttpHeadersCodec();

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public StandardQuasiHttpServer()
        {
        }

        /// <summary>
        /// Gets or sets an instance of the <see cref="IQuasiHttpApplication"/> type which is
        /// responsible for processing requests to generate responses.
        /// </summary>
        public virtual IQuasiHttpApplication Application { get; set; }

        /// <summary>
        /// Gets or sets the underlying transport (TCP or IPC) for retrieving requests
        /// for quasi web applications, and for sending responses generated from quasi web applications.
        /// </summary>
        public virtual IQuasiHttpServerTransport Transport { get; set; }

        /// <summary>
        /// Used to process incoming connections from quasi http server transports.
        /// </summary>
        /// <param name="connection">represents a connection and any associated information</param>
        /// <returns>a task representing asynchronous operation</returns>
        public async Task AcceptConnection(IConnectionAllocationResponse connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var transport = Transport;

            try
            {
                await ProcessReceive(Application, Transport,
                    connection);
                await Abort(transport, connection, false);
            }
            catch (Exception e)
            {
                await Abort(transport, connection, true);
                if (e is QuasiHttpRequestProcessingException)
                {
                    throw;
                }
                var abortError = new QuasiHttpRequestProcessingException(
                    "encountered error during receive request processing",
                    QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                    e);
                throw abortError;
            }
        }
        internal static async Task ProcessReceive(
            IQuasiHttpApplication application,
            IQuasiHttpTransport transport,
            IConnectionAllocationResponse connection)
        {
            if (transport == null)
            {
                throw new MissingDependencyException("server transport");
            }
            if (application == null)
            {
                throw new MissingDependencyException("server application");
            }

            var request = await ReadRequest(transport, connection);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }

            var response = await application.ProcessRequest(request);
            if (response == null)
            {
                throw new QuasiHttpRequestProcessingException("no response");
            }

            try
            {
                await WriteResponse(response, transport, connection);
            }
            finally
            {
                var disposer = response.Disposer;
                if (disposer != null)
                {
                    await disposer();
                }
            }
        }

        private static async Task<IQuasiHttpRequest> ReadRequest(
            IQuasiHttpTransport transport,
            IConnectionAllocationResponse connection)
        {
            var encodedRequest = await transport.Read(connection, false);
            if (encodedRequest?.Headers == null)
            {
                return null;
            }

            var headers = HeadersCodec.DecodeRequestHeaders(
                encodedRequest.Headers);
            var request = new DefaultQuasiHttpRequest
            {
                Environment = connection.Environment,
                Headers = headers
            };
            request.Body = ProtocolUtilsInternal.CreateBodyFromTransport(
                headers.ContentLength,
                encodedRequest.Body);
            return request;
        }

        private static async Task WriteResponse(
            IQuasiHttpResponse response,
            IQuasiHttpTransport transport,
            IConnectionAllocationResponse connection)
        {
            var headers = response.Headers;
            if (headers == null)
            {
                return;
            }

            var encodedHeaders = HeadersCodec.EncodeResponseHeaders(headers,
                connection.ProcessingOptions?.MaxHeadersSize);

            var responseBodyReader = ProtocolUtilsInternal.CreateReaderToTransport(
                headers.ContentLength, response.Body);
            await transport.Write(connection,
                encodedHeaders, responseBodyReader, true);
        }

        internal static async Task Abort(IQuasiHttpTransport transport,
            IConnectionAllocationResponse connection, bool errorOccured)
        {
            if (!errorOccured)
            {
                await transport.ReleaseConnection(connection);
            }
            else
            {
                try
                {
                    // don't wait.
                    _ = transport.ReleaseConnection(connection);
                }
                catch (Exception) { } // ignore
            }
        }
    }
}
