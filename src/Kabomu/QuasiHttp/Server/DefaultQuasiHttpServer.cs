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
    public class DefaultQuasiHttpServer : IQuasiHttpServer
    {
        private readonly ISet<ReceiveTransferInternal> _transfers;
        private readonly Func<object, Exception, Task> AbortTransferCallback;
        private readonly Func<object, Exception, IQuasiHttpResponse, Task> AbortTransferCallback2;
        private bool _running;

        public DefaultQuasiHttpServer()
        {
            AbortTransferCallback = CancelReceive;
            AbortTransferCallback2 = CancelReceive;
            _transfers = new HashSet<ReceiveTransferInternal>();
            MutexApi = new LockBasedMutexApi();
            TimerApi = new DefaultTimerApi();
        }

        public IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpServerTransport Transport { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IMutexApi MutexApi { get; set; }
        public ITimerApi TimerApi { get; set; }

        private Task CancelReceive(object transferObj, Exception cancellationError)
        {
            var transfer = (ReceiveTransferInternal)transferObj;
            return AbortTransfer(transfer, cancellationError, null);
        }

        private Task CancelReceive(object transferObj, Exception cancellationError,
            IQuasiHttpResponse res)
        {
            var transfer = (ReceiveTransferInternal)transferObj;
            return AbortTransfer(transfer, cancellationError, res);
        }

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

        public Task<IQuasiHttpResponse> ProcessReceiveRequest(IQuasiHttpRequest request, IQuasiHttpProcessingOptions options)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
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
                await timerApi.SetTimeout(resetTimeMillis, () => Reset(null)).Item1;
            }
            else
            {
                await Reset(null);
            }
        }

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
            transfer.TimeoutId = timer.SetTimeout(transfer.TimeoutMillis, async () =>
            {
                await AbortTransfer(transfer, new Exception("receive timeout"), null);
            }).Item2;
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
                await transfer.Transport.ReleaseConnection(transfer.Connection);
            }
            if (transfer.Request?.Body != null)
            {
                await transfer.Request.Body.EndRead();
            }
        }
    }
}
