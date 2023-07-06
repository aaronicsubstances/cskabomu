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
    public class StandardQuasiHttpServer : IQuasiHttpServer
    {
        private readonly object _mutex = new object();
        private bool _running;

        /// <summary>
        /// Creates a new instance of the <see cref="StandardQuasiHttpServer"/> class with defaults provided
        /// for the <see cref="MutexApi"/> and <see cref="TimerApi"/> properties.
        /// </summary>
        public StandardQuasiHttpServer()
        {
            DefaultProtocolFactory = transfer =>
            {
                return new DefaultReceiveProtocolInternal
                {
                    MaxChunkSize = transfer.MaxChunkSize,
                    Application = Application,
                    Transport = Transport,
                    Connection = transfer.Connection,
                    RequestEnvironment = transfer.RequestEnvironment
                };
            };
            AltProtocolFactory = transfer =>
            {
                return new AltReceiveProtocolInternal
                {
                    Application = Application,
                    Request = transfer.Request
                };
            };
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
        /// Gets or sets a callback which can be used to report errors of processing requests
        /// received from connections.
        /// </summary>
        public UncaughtErrorCallback ErrorHandler { get; set; }

        /// <summary>
        /// Exposed for testing.
        /// </summary>
        internal Func<ReceiveTransferInternal, IReceiveProtocolInternal> DefaultProtocolFactory { get; set; }

        /// <summary>
        /// Exposed for testing.
        /// </summary>
        internal Func<ReceiveTransferInternal, IReceiveProtocolInternal> AltProtocolFactory { get; set; }

        /// <summary>
        /// Starts the instance set in the <see cref="Transport"/> property and begins
        /// receiving connections from it.
        /// </summary>
        /// <remarks>
        /// Server starting is not required to process requests directly with the <see cref="Application"/>
        /// property and the <see cref="ProcessReceiveRequest(IQuasiHttpRequest, IQuasiHttpProcessingOptions)"/>
        /// method.
        /// <para></para>
        /// A call to this method will be ignored if its instance is already started and running.
        /// </remarks>
        /// <returns>a task representing asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/> property is null.</exception>
        public async Task Start()
        {
            Task startTask;
            lock (_mutex)
            {
                if (_running)
                {
                    return;
                }
                startTask = Transport?.Start();
            }
            if (startTask == null)
            {
                throw new MissingDependencyException("transport");
            }
            await startTask;
            // let error handler or TaskScheduler.UnobservedTaskException handle 
            // any uncaught task exceptions.
            _ = StartAcceptingConnections();
        }

        /// <summary>
        /// Stops the instance set in the <see cref="Transport"/> property and stops receiving connections from it.
        /// </summary>
        /// <remarks>
        /// A call to this method will be ignored if its instance is already started and running.
        /// </remarks>
        /// <param name="waitTimeMillis">if nonnegative, then it indicates the delay in milliseconds
        /// from the current time in which to wait for any ongoing processing to complete.</param>
        /// <returns>a task representing the asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/> property is null.</exception>
        /// <exception cref="MissingDependencyException">The <paramref name="waitTimeMillis"/> argument is positive but
        /// the <see cref="TimerApi"/> property needed is null.</exception>
        public async Task Stop(int waitTimeMillis)
        {
            Task stopTask;
            lock (_mutex)
            {
                if (!_running)
                {
                    return;
                }
                _running = false;
                stopTask = Transport?.Stop();
            }
            if (stopTask == null)
            {
                throw new MissingDependencyException("transport");
            }
            await stopTask;
            if (waitTimeMillis > 0)
            {
                await Task.Delay(waitTimeMillis);
            }
        }

        private async Task StartAcceptingConnections()
        {
            lock (_mutex)
            {
                _running = true;
            }
            try
            {
                while (true)
                {
                    Task<IConnectionAllocationResponse> connectTask;
                    lock (_mutex)
                    {
                        if (!_running)
                        {
                            break;
                        }
                        connectTask = Transport?.ReceiveConnection();
                    }
                    if (connectTask == null)
                    {
                        throw new MissingDependencyException("transport");
                    }
                    var connectionAllocationResponse = await connectTask;
                    if (connectionAllocationResponse?.Connection == null)
                    {
                        throw new QuasiHttpRequestProcessingException("no connection");
                    }
                    // let TaskScheduler.UnobservedTaskException handle any uncaught task exceptions.
                    _ = AcceptConnection(connectionAllocationResponse);
                }
            }
            catch (Exception e)
            {
                var eh = ErrorHandler;
                if (eh == null)
                {
                    throw;
                }
                eh.Invoke(e, "error encountered while accepting connections");
            }
        }

        private async Task AcceptConnection(IConnectionAllocationResponse connectionAllocationResponse)
        {
            try
            {
                await Receive(connectionAllocationResponse);
            }
            catch (Exception e)
            {
                var eh = ErrorHandler;
                if (eh == null)
                {
                    throw;
                }
                eh.Invoke(e, "error encountered while receiving a connection");
            }
        }

        private async Task Receive(IConnectionAllocationResponse connectionResponse)
        {
            var transfer = new ReceiveTransferInternal
            {
                Mutex = _mutex
            };
            QuasiHttpRequestProcessingException abortError = null;
            try
            {
                await Receive(transfer, connectionResponse);
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
                        "encountered error during receive request processing", e);
                }
            }
            await transfer.Abort(null);
            if (abortError != null)
            {
                throw abortError;
            }
        }

        private async Task Receive(ReceiveTransferInternal transfer,
            IConnectionAllocationResponse connectionResponse)
        {
            Task<IQuasiHttpResponse> workTask;
            Task<IQuasiHttpResponse> timeoutTask;
            lock (_mutex)
            {
                var timeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    null, DefaultProcessingOptions?.TimeoutMillis, 0);
                (timeoutTask, transfer.TimeoutId) = ProtocolUtilsInternal.SetTimeout<IQuasiHttpResponse>(timeoutMillis);

                transfer.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    null, DefaultProcessingOptions?.MaxChunkSize, 0);

                transfer.Connection = connectionResponse.Connection;
                transfer.RequestEnvironment = connectionResponse.Environment;

                workTask = transfer.StartProtocol(DefaultProtocolFactory);
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
        /// request processing.
        /// </remarks>
        /// <param name="request">quasi http request to process </param>
        /// <param name="options">supplies request timeout and any processing options which should 
        /// override the default processing options</param>
        /// <returns>a task whose result will be the response generated by the quasi http application</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Application"/> property is null.</exception>
        public async Task<IQuasiHttpResponse> ProcessReceiveRequest(IQuasiHttpRequest request, IQuasiHttpProcessingOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var transfer = new ReceiveTransferInternal
            {
                Mutex = _mutex,
                Request = request,
            };
            IQuasiHttpResponse res = null;
            QuasiHttpRequestProcessingException abortError = null;
            try
            {
                res = await ProcessReceiveRequest(transfer, options);
                if (res == null)
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
                        "encountered error during receive request processing", e);
                }
            }
            await transfer.Abort(res);
            if (abortError != null)
            {
                throw abortError;
            }
            return res;
        }

        private async Task<IQuasiHttpResponse> ProcessReceiveRequest(
            ReceiveTransferInternal transfer, IQuasiHttpProcessingOptions options)
        {
            Task<IQuasiHttpResponse> workTask;
            Task<IQuasiHttpResponse> timeoutTask;
            lock (_mutex)
            {
                var timeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    options?.TimeoutMillis, DefaultProcessingOptions?.TimeoutMillis, 0);
                (timeoutTask, transfer.TimeoutId) = ProtocolUtilsInternal.SetTimeout<IQuasiHttpResponse>(timeoutMillis);

                workTask = transfer.StartProtocol(AltProtocolFactory);
            }
            return await ProtocolUtilsInternal.CompleteRequestProcessing(workTask,
                timeoutTask, null);
        }
    }
}
