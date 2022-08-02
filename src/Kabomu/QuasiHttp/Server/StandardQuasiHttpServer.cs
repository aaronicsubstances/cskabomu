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
        private readonly ISet<ReceiveTransferInternal> _transfers;
        private readonly Func<object, Task> AbortTransferCallback;
        private readonly Func<object, IQuasiHttpResponse, Task> AbortTransferCallback2;
        private bool _running;

        /// <summary>
        /// Creates a new instance of the <see cref="StandardQuasiHttpServer"/> class with defaults provided
        /// for the <see cref="MutexApi"/> and <see cref="TimerApi"/> properties.
        /// </summary>
        public StandardQuasiHttpServer()
        {
            AbortTransferCallback = CancelReceive;
            AbortTransferCallback2 = CancelReceive;
            _transfers = new HashSet<ReceiveTransferInternal>();
            MutexApi = new LockBasedMutexApi();
            TimerApi = new DefaultTimerApi();
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
        /// Gets or sets mutex api used to guard multithreaded 
        /// access to connection acceptance operations of this class.
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

        private Task CancelReceive(object transferObj)
        {
            var transfer = (ReceiveTransferInternal)transferObj;
            return AbortTransfer(transfer, null, null);
        }

        private Task CancelReceive(object transferObj, IQuasiHttpResponse res)
        {
            var transfer = (ReceiveTransferInternal)transferObj;
            return AbortTransfer(transfer, null, res);
        }

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
            using (await MutexApi.Synchronize())
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
        /// Stops the instance set in the <see cref="Transport"/> property, stops receiving connections from it,
        /// and optionally calls the <see cref="Reset(Exception)"/> method.
        /// </summary>
        /// <remarks>
        /// A call to this method will be ignored if its instance is already started and running.
        /// </remarks>
        /// <param name="resetTimeMillis">if nonnegative, then it indicates the delay in milliseconds
        /// from the current time after which all ongoing connections and request processing will be forcefully reset.
        /// NB: a zero value will lead to immediate resetting of connections and requests without any delay.</param>
        /// <returns>a task representing the asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Transport"/> property is null.</exception>
        /// <exception cref="MissingDependencyException">The <paramref name="resetTimeMillis"/> argument is positive but
        /// the <see cref="TimerApi"/> property needed to schedule the call to <see cref="Reset(Exception)"/> is null.</exception>
        public async Task Stop(int resetTimeMillis)
        {
            ITimerApi timerApi;
            Task stopTask;
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    return;
                }
                _running = false;
                timerApi = TimerApi;
                stopTask = Transport?.Stop();
            }
            if (stopTask == null)
            {
                throw new MissingDependencyException("transport");
            }
            await stopTask;
            if (resetTimeMillis < 0)
            {
                return;
            }
            if (resetTimeMillis > 0)
            {
                if (timerApi == null)
                {
                    throw new MissingDependencyException("timer api");
                }
                await timerApi.WhenSetTimeout(() => Reset(null), resetTimeMillis).Item1;
            }
            else
            {
                await Reset(null);
            }
        }

        private async Task StartAcceptingConnections()
        {
            using (await MutexApi.Synchronize())
            {
                _running = true;
            }
            try
            {
                while (true)
                {
                    Task<IConnectionAllocationResponse> connectTask;
                    IQuasiHttpServerTransport transport;
                    using (await MutexApi.Synchronize())
                    {
                        if (!_running)
                        {
                            break;
                        }
                        transport = Transport;
                        if (transport == null)
                        {
                            throw new MissingDependencyException("transport");
                        }
                        connectTask = transport.ReceiveConnection();
                    }
                    var connectionAllocationResponse = await connectTask;
                    if (connectionAllocationResponse == null)
                    {
                        throw new Exception("received null for connection allocation response");
                    }
                    // let TaskScheduler.UnobservedTaskException handle any uncaught task exceptions.
                    _ = AcceptConnection(transport, connectionAllocationResponse);
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

        private async Task AcceptConnection(IQuasiHttpTransport transport, IConnectionAllocationResponse connectionAllocationResponse)
        {
            try
            {
                await Receive(transport, connectionAllocationResponse);
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

        private async Task Receive(IQuasiHttpTransport transport, IConnectionAllocationResponse connectionResponse)
        {
            if (connectionResponse?.Connection == null)
            {
                throw new ArgumentException("null connection");
            }

            var transfer = new ReceiveTransferInternal
            {
                Transport = transport,
                Connection = connectionResponse.Connection,
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };

            Task workTask;
            using (await MutexApi.Synchronize())
            {
                transfer.TimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    null, DefaultProcessingOptions?.TimeoutMillis, 0);
                SetReceiveTimeout(transfer);

                _transfers.Add(transfer);

                transfer.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    null, DefaultProcessingOptions?.MaxChunkSize, TransportUtils.DefaultMaxChunkSize);

                transfer.RequestEnvironment = ProtocolUtilsInternal.DetermineEffectiveOptions(
                    connectionResponse.Environment, DefaultProcessingOptions?.RequestEnvironment);

                var protocol = new DefaultReceiveProtocolInternal
                {
                    Parent = transfer,
                    AbortCallback = AbortTransferCallback,
                    MaxChunkSize = transfer.MaxChunkSize,
                    Application = Application,
                    Transport = transfer.Transport,
                    Connection = transfer.Connection,
                    RequestEnvironment = transfer.RequestEnvironment
                };
                workTask = protocol.Receive();
            }

            var firstCompletedTask = await Task.WhenAny(transfer.CancellationTcs.Task, workTask);
            try
            {
                await firstCompletedTask;
            }
            catch (Exception e)
            {
                // let call to abort transfer determine whether exception is significant.
                await AbortTransfer(transfer, e, null);
            }

            // by awaiting again for transfer cancellation, any significant error will bubble up, and
            // any insignificant error will be swallowed.
            await transfer.CancellationTcs.Task;
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
        public Task<IQuasiHttpResponse> ProcessReceiveRequest(IQuasiHttpRequest request, IQuasiHttpProcessingOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var transfer = new ReceiveTransferInternal
            {
                Request = request,
                ProcessingOptions = options,
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            return ProcessSendToApplication(transfer);
        }

        private async Task<IQuasiHttpResponse> ProcessSendToApplication(ReceiveTransferInternal transfer)
        {
            Task workTask;
            using (await MutexApi.Synchronize())
            {
                transfer.TimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    transfer.ProcessingOptions?.TimeoutMillis, DefaultProcessingOptions?.TimeoutMillis, 0);
                SetReceiveTimeout(transfer);

                _transfers.Add(transfer);

                transfer.RequestEnvironment = ProtocolUtilsInternal.DetermineEffectiveOptions(
                    transfer.ProcessingOptions?.RequestEnvironment, DefaultProcessingOptions?.RequestEnvironment);

                var protocol = new AltReceiveProtocolInternal
                {
                    Parent = transfer,
                    AbortCallback = AbortTransferCallback2,
                    Application = Application,
                    RequestEnvironment = transfer.RequestEnvironment
                };
                workTask = protocol.SendToApplication(transfer.Request);
            }

            var firstCompletedTask = await Task.WhenAny(transfer.CancellationTcs.Task, workTask);
            try
            {
                await firstCompletedTask;
            }
            catch (Exception e)
            {
                // let call to abort transfer determine whether exception is significant.
                await AbortTransfer(transfer, e, null);
            }

            return await transfer.CancellationTcs.Task;
        }

        /// <summary>
        /// Releases all ongoing connections and terminates all ongoing request processing.
        /// </summary>
        /// <remarks>
        /// This method can be called at any time regardless of whether an instace of this class has been started or stopped.
        /// </remarks>
        /// <param name="cause">the error message which will be used to terminate ongoing request processing. Can be
        /// null in which case error with message of "server reset" will be used.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Reset(Exception cause)
        {
            var cancellationException = cause ?? new Exception("server reset");

            // since it is desired to clear all pending transfers under lock,
            // and disabling of transfer is an async transfer, we choose
            // not to await on each disabling, but rather to wait on them
            // after clearing the transfers.
            var tasks = new List<Task>();
            using (await MutexApi.Synchronize())
            {
                try
                {
                    foreach (var transfer in _transfers)
                    {
                        tasks.Add(DisableTransfer(transfer, cancellationException, null));
                    }
                }
                finally
                {
                    _transfers.Clear();
                }
            }

            await Task.WhenAll(tasks);
        }

        private void SetReceiveTimeout(ReceiveTransferInternal transfer)
        {
            if (transfer.TimeoutMillis <= 0)
            {
                return;
            }
            var timer = TimerApi;
            if (timer == null)
            {
                throw new MissingDependencyException("timer api");
            }
            transfer.TimeoutId = timer.WhenSetTimeout(async () =>
            {
                await AbortTransfer(transfer, new Exception("receive timeout"), null);
            }, transfer.TimeoutMillis).Item2;
        }

        private async Task AbortTransfer(ReceiveTransferInternal transfer, Exception cancellationError,
            IQuasiHttpResponse res)
        {
            Task disableTransferTask;
            using (await MutexApi.Synchronize())
            {
                if (transfer.IsAborted)
                {
                    return;
                }
                _transfers.Remove(transfer);
                disableTransferTask = DisableTransfer(transfer, cancellationError, res);
            }
            await disableTransferTask;
        }

        private async Task DisableTransfer(ReceiveTransferInternal transfer, Exception cancellationError,
            IQuasiHttpResponse res)
        {
            if (cancellationError != null)
            {
                transfer.CancellationTcs.SetException(cancellationError);
            }
            else
            {
                transfer.CancellationTcs.SetResult(res);
            }
            transfer.IsAborted = true;
            TimerApi?.ClearTimeout(transfer.TimeoutId);
            if (transfer.Connection != null)
            {
                try
                {
                    await transfer.Transport.ReleaseConnection(transfer.Connection);
                }
                catch (Exception) { }
            }
            else
            {
                // close body of send to application request
                if (transfer.Request?.Body != null)
                {
                    try
                    {
                        await transfer.Request.Body.EndRead();
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
