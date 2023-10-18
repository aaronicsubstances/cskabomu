using Kabomu.Abstractions;
using Kabomu.Exceptions;
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
        /// <param name="connection">represents a quasi http connection</param>
        /// <returns>a task representing asynchronous operation</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="connection"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/>
        /// property or <see cref="Application"/> property is null.</exception>
        /// <exception cref="QuasiHttpException">An error occured with request processing</exception>
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
                var timeoutScheduler = connection.TimeoutScheduler;
                if (timeoutScheduler != null)
                {
                    Func<Task<IQuasiHttpResponse>> proc = () => ProcessAccept(
                        application, transport, connection);
                    await ProtocolUtilsInternal.RunTimeoutScheduler(
                        timeoutScheduler, false, proc);
                }
                else
                {
                    var acceptTask = ProcessAccept(application, transport,
                        connection);
                    var timeoutTask = connection.TimeoutTask;
                    if (timeoutTask != null)
                    {
                        await await Task.WhenAny(acceptTask,
                            ProtocolUtilsInternal.WrapTimeoutTask(
                                timeoutTask, false));
                    }
                    await acceptTask;
                }
                await Abort(transport, connection, false);
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

        private static async Task<IQuasiHttpResponse> ProcessAccept(
            QuasiHttpApplication application,
            IQuasiHttpServerTransport transport,
            IQuasiHttpConnection connection)
        {
            IQuasiHttpRequest request = null;
            var altTransport = transport as IQuasiHttpAltTransport;
            var requestDeserializer = altTransport?.RequestDeserializer;
            if (requestDeserializer != null)
            {
                request = await requestDeserializer(connection);
            }
            if (request == null)
            {
                request = (IQuasiHttpRequest)await ProtocolUtilsInternal.ReadEntityFromTransport(
                    false, transport.GetReadableStream(connection), connection);
            }

            await using var response = await application(request);
            if (response == null)
            {
                throw new QuasiHttpException("no response");
            }

            var responseSerialized = false;
            var responseSerializer = altTransport?.ResponseSerializer;
            if (responseSerializer != null)
            {
                responseSerialized = await responseSerializer(connection, response);
            }
            if (!responseSerialized)
            {
                await ProtocolUtilsInternal.WriteEntityToTransport(
                    true, response, transport.GetWritableStream(connection),
                    connection);
            }

            return null;
        }

        private static async Task Abort(IQuasiHttpServerTransport transport,
            IQuasiHttpConnection connection, bool errorOccured)
        {
            if (errorOccured)
            {
                try
                {
                    // don't wait.
                    _ = transport.ReleaseConnection(connection)
                        .ContinueWith(_ => { }); // swallow errors.
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
