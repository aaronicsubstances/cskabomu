using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    /// <summary>
    /// The standard implementation of the server side of the quasi http protocol defined by the Kabomu library.
    /// </summary>
    /// <remarks>
    /// This class provides the server facing side of networking for end users. It is the complement to the 
    /// <see cref="StandardQuasiHttpClient"/> class for providing HTTP semantics
    /// whiles enabling underlying transport options beyond TCP.
    /// <para></para>
    /// Therefore this class can be seen as the equivalent of an HTTP server in which the underlying transport of
    /// choice extends beyond TCP to include IPC mechanisms.
    /// </remarks>
    public class StandardQuasiHttpServer
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public StandardQuasiHttpServer()
        {
        }

        /// <summary>
        /// Gets or sets the function which is
        /// responsible for processing requests to generate responses.
        /// </summary>
        public virtual QuasiHttpApplication Application { get; set; }

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
        public async Task AcceptConnection(IQuasiHttpConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            // access fields for use per processing call, in order to cooperate with
            // any implementation of field accessors which supports
            // concurrent modifications.
            var transport = Transport;
            var application = Application;
            if (transport == null)
            {
                throw new MissingDependencyException("server transport");
            }
            if (application == null)
            {
                throw new MissingDependencyException("server application");
            }

            try
            {
                var acceptTask = ProcessAccept(application, transport,
                    connection);
                if (connection.TimeoutTask != null)
                {
                    var timeoutTask = ProtocolUtilsInternal.WrapTimeoutTask(
                        connection.TimeoutTask, "receive timeout");
                    await ProtocolUtilsInternal.CompleteWorkTask(
                        acceptTask, timeoutTask);
                }
                else
                {
                    await acceptTask;
                }
            }
            catch (Exception e)
            {
                await Abort(transport, connection, true);
                if (e is QuasiHttpException)
                {
                    throw;
                }
                var abortError = new QuasiHttpException(
                    "encountered error during receive request processing",
                    QuasiHttpException.ReasonCodeGeneral,
                    e);
                throw abortError;
            }
        }

        internal static async Task ProcessAccept(
            QuasiHttpApplication application,
            IQuasiHttpServerTransport transport,
            IQuasiHttpConnection connection)
        {
            var (encodedRequestBody, encodedRequestHeaders) =
                await ProtocolUtilsInternal.ReadEntityFromTransport(
                    false, transport, connection);

            var request = new DefaultQuasiHttpRequest
            {
                Environment = connection.Environment
            };
            QuasiHttpCodec.DecodeRequestHeaders(encodedRequestHeaders, 0,
                encodedRequestHeaders.Length, request);
            request.Body = ProtocolUtilsInternal.DecodeRequestBodyFromTransport(
                request.ContentLength, encodedRequestBody);

            var response = await application(request);
            if (response == null)
            {
                throw new QuasiHttpException("no response");
            }

            try
            {
                if (ProtocolUtilsInternal.GetEnvVarAsBoolean(response.Environment,
                    QuasiHttpUtils.EnvKeySkipSending) != true)
                {
                    var encodedResponseHeaders = QuasiHttpCodec.EncodeResponseHeaders(response,
                        connection.ProcessingOptions?.MaxHeadersSize);
                    var encodedResponseBody = ProtocolUtilsInternal.EncodeBodyToTransport(true,
                        response.ContentLength, response.Body);
                    await transport.Write(connection, true, encodedResponseHeaders,
                        encodedResponseBody);
                }
            }
            finally
            {
                var disposer = response.Disposer;
                if (disposer != null)
                {
                    await disposer();
                }
            }
            await Abort(transport, connection, false);
        }

        internal static async Task Abort(IQuasiHttpServerTransport transport,
            IQuasiHttpConnection connection, bool errorOccured)
        {
            if (errorOccured)
            {
                try
                {
                    // don't wait.
                    _ = transport.ReleaseConnection(connection);
                }
                catch (Exception) { } // ignore
            }
            else
            {
                await transport.ReleaseConnection(connection);
            }
        }
    }
}
