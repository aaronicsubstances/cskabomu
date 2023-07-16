using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    /// <summary>
    /// The standard implementation of the server side of the quasi http protocol defined by the Kabomu library.
    /// </summary>
    /// <remarks>
    /// This clas provides the server facing side of networking for end users. It is the complement to the 
    /// <see cref="Client.StandardQuasiHttpClient"/> class for providing HTTP semantics for web application frameworks
    /// whiles enabling underlying transport options beyond TCP.
    /// <para></para>
    /// Therefore this class can be seen as the equivalent of an HTTP server in which the underlying transport of
    /// choice extends beyond TCP to include IPC mechanisms.
    /// </remarks>
    public class StandardQuasiHttpServer
    {
        private readonly object _mutex = new object();

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public StandardQuasiHttpServer()
        {
        }

        /// <summary>
        /// Gets or sets the default options used to process receive requests.
        /// </summary>
        public IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }

        /// <summary>
        /// Gets or sets an instance of the <see cref="IQuasiHttpApplication"/> type which is
        /// responsible for processing requests to generate responses.
        /// </summary>
        public IQuasiHttpApplication Application { get; set; }

        /// <summary>
        /// Gets or sets the underlying transport (TCP or IPC) for retrieving requests
        /// for quasi web applications, and for sending responses generated from quasi web applications.
        /// </summary>
        public IQuasiHttpServerTransport Transport { get; set; }

        /// <summary>
        /// Used to process incoming connections from quasi http server transports.
        /// </summary>
        /// <param name="connectionAllocationResponse">represents a connection and any associated information</param>
        /// <returns>a task representing asynchronous operation</returns>
        public async Task AcceptConnection(IConnectionAllocationResponse connectionAllocationResponse)
        {
            if (connectionAllocationResponse == null)
            {
                throw new ArgumentNullException(nameof(connectionAllocationResponse));
            }
            var transfer = new ReceiveTransferInternal();
            try
            {
                await ProcessAcceptConnection(transfer, connectionAllocationResponse);
            }
            catch (Exception e)
            {
                await transfer.Abort(null);
                if (e is QuasiHttpRequestProcessingException)
                {
                    throw;
                }
                else
                {
                    var abortError = new QuasiHttpRequestProcessingException(
                        QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                        "encountered error during receive request processing", e);
                    throw abortError;
                }
            }
        }

        private async Task ProcessAcceptConnection(ReceiveTransferInternal transfer,
            IConnectionAllocationResponse connectionResponse)
        {
            Task<IQuasiHttpResponse> workTask;
            Task<IQuasiHttpResponse> timeoutTask;
            lock (_mutex)
            {
                var timeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    null, DefaultProcessingOptions?.TimeoutMillis, 0);
                (timeoutTask, transfer.TimeoutId) = ProtocolUtilsInternal.SetTimeout<IQuasiHttpResponse>(timeoutMillis,
                    "receive timeout");

                int maxChunkSize = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    null, DefaultProcessingOptions?.MaxChunkSize, 0);

                var protocol = new DefaultReceiveProtocolInternal
                {
                    MaxChunkSize = maxChunkSize,
                    Application = Application,
                    Transport = Transport,
                    Connection = connectionResponse.Connection,
                    RequestEnvironment = connectionResponse.Environment
                };
                workTask = transfer.StartProtocol(protocol);
            }
            await ProtocolUtilsInternal.CompleteRequestProcessing(workTask,
                timeoutTask, null);
        }

        /// <summary>
        /// Sends a quasi http request directly to the <see cref="Application"/> property within some
        /// timeout value.
        /// </summary>
        /// <remarks>
        /// By this method, transport types which are not connection-oriented or implement connections
        /// differently can still make use of this class to offload some of the burdens of quasi http
        /// request processing, such as setting timeouts on request processing.
        /// </remarks>
        /// <param name="request">quasi http request to process </param>
        /// <param name="options">supplies request timeout and any processing options which should 
        /// override the default processing options</param>
        /// <returns>a task whose result will be the response generated by the quasi http application</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Application"/> property is null.</exception>
        public async Task<IQuasiHttpResponse> AcceptRequest(IQuasiHttpRequest request, IQuasiHttpProcessingOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var transfer = new ReceiveTransferInternal
            {
                Request = request,
            };
            IQuasiHttpResponse res = null;
            try
            {
                res = await ProcessAcceptRequest(transfer, options);
                if (res == null)
                {
                    throw new ExpectationViolationException("expected non-null response");
                }
            }
            catch (Exception e)
            {
                await transfer.Abort(res);
                if (e is QuasiHttpRequestProcessingException)
                {
                    throw;
                }
                else
                {
                    var abortError = new QuasiHttpRequestProcessingException(
                        QuasiHttpRequestProcessingException.ReasonCodeGeneral,
                        "encountered error during receive request processing", e);
                    throw abortError;
                }
            }
            return res;
        }

        private async Task<IQuasiHttpResponse> ProcessAcceptRequest(
            ReceiveTransferInternal transfer, IQuasiHttpProcessingOptions options)
        {
            Task<IQuasiHttpResponse> workTask;
            Task<IQuasiHttpResponse> timeoutTask;
            lock (_mutex)
            {
                var timeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    options?.TimeoutMillis, DefaultProcessingOptions?.TimeoutMillis, 0);
                (timeoutTask, transfer.TimeoutId) = ProtocolUtilsInternal.SetTimeout<IQuasiHttpResponse>(timeoutMillis,
                    "receive timeout");

                var protocol = new AltReceiveProtocolInternal
                {
                    Application = Application,
                    Request = transfer.Request
                };
                workTask = transfer.StartProtocol(protocol);
            }
            return await ProtocolUtilsInternal.CompleteRequestProcessing(workTask,
                timeoutTask, null);
        }
    }
}
