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

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpServer : IQuasiHttpServer
    {
        private readonly ISet<ReceiveTransferInternal> _transfers;
        private bool _running;
        private readonly Func<ReceiveTransferInternal, Exception, IQuasiHttpResponse, Task> AbortTransferCallback;

        public DefaultQuasiHttpServer()
        {
            AbortTransferCallback = AbortTransfer;
            _transfers = new HashSet<ReceiveTransferInternal>();
            MutexApi = new LockBasedMutexApi();
            MutexApiFactory = null;
            TimerApi = new DefaultTimerApi();
        }

        public IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpServerTransport Transport { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IMutexApi MutexApi { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }
        public ITimerApi TimerApi { get; set; }

        public async Task Start()
        {
            Task startTask;
            using (await MutexApi.Synchronize())
            {
                if (_running)
                {
                    return;
                }
                _running = true;
                startTask = Transport.Start();
            }
            await startTask;
            // let error handler or TaskScheduler.UnobservedTaskException handle 
            // any uncaught task exceptions.
            _ = StartAcceptingConnections();
        }

        private async Task StartAcceptingConnections()
        {
            try
            {
                while (true)
                {
                    Task<IConnectionAllocationResponse> connectTask;
                    using (await MutexApi.Synchronize())
                    {
                        if (!_running)
                        {
                            break;
                        }
                        var transport = Transport;
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

        private async Task Receive(IConnectionAllocationResponse connectionAllocationResponse)
        {
            if (connectionAllocationResponse == null)
            {
                throw new ArgumentException("null connection allocation");
            }
            if (connectionAllocationResponse.Connection == null)
            {
                throw new ArgumentException("null connection");
            }

            var transfer = new ReceiveTransferInternal
            {
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };

            IMutexApi transferMutex = await ProtocolUtilsInternal.DetermineEffectiveMutexApi(
                connectionAllocationResponse.ProcessingMutexApi, MutexApiFactory);

            Task workTask;
            using (await MutexApi.Synchronize())
            {
                _transfers.Add(transfer);

                int transferTimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                    null, DefaultProcessingOptions?.OverallReqRespTimeoutMillis, 0);
                SetResponseTimeout(transfer, transferTimeoutMillis);

                int protocolMaxChunkSize = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(
                    null, DefaultProcessingOptions?.MaxChunkSize, TransportUtils.DefaultMaxChunkSize);

                var effectiveRequestEnvironment = ProtocolUtilsInternal.DetermineEffectiveOptions(
                    connectionAllocationResponse.Environment, DefaultProcessingOptions?.RequestEnvironment);

                transfer.Protocol = new ReceiveProtocolInternal
                {
                    Parent = transfer,
                    AbortCallback = AbortTransferCallback,
                    MaxChunkSize = protocolMaxChunkSize,
                    Application = Application,
                    Transport = Transport,
                    Connection = connectionAllocationResponse.Connection,
                    RequestEnvironment = effectiveRequestEnvironment,
                    MutexApi = transferMutex
                };
                workTask = transfer.Protocol.Receive();
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

        public async Task<IQuasiHttpResponse> SendToApplication(IQuasiHttpRequest request, IQuasiHttpProcessingOptions options)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
            }
            var transfer = new ReceiveTransferInternal
            {
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            Task workTask;
            using (await MutexApi.Synchronize())
            {
                _transfers.Add(transfer);

                int transferTimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                    options?.OverallReqRespTimeoutMillis, DefaultProcessingOptions?.OverallReqRespTimeoutMillis, 0);
                SetResponseTimeout(transfer, transferTimeoutMillis);

                workTask = CompleteProcessWithApplication(options, transfer, request);
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

        private async Task CompleteProcessWithApplication(IQuasiHttpProcessingOptions options,
            ReceiveTransferInternal transfer, IQuasiHttpRequest request)
        {
            var destApp = Application;
            if (destApp == null)
            {
                throw new MissingDependencyException("application");
            }

            var effectiveRequestEnvironment = ProtocolUtilsInternal.DetermineEffectiveOptions(
                options?.RequestEnvironment, DefaultProcessingOptions?.RequestEnvironment);

            var res = await destApp.ProcessRequest(request, effectiveRequestEnvironment);

            if (res == null)
            {
                throw new Exception("no response");
            }

            await AbortTransfer(transfer, null, res);
        }

        public async Task Stop()
        {
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    return;
                }
                _running = false;
            }
            await Reset();
            await Transport.Stop();
        }

        private async Task Reset()
        {
            var cancellationException = new Exception("server reset");

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

        private void SetResponseTimeout(ReceiveTransferInternal transfer, int transferTimeoutMillis)
        {
            if (transferTimeoutMillis <= 0)
            {
                return;
            }
            var timer = TimerApi;
            if (timer == null)
            {
                throw new MissingDependencyException("timer api");
            }
            transfer.TimeoutId = timer.SetTimeout(transferTimeoutMillis, async () =>
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
            if (transfer.Protocol != null)
            {
                await transfer.Protocol.Cancel();
            }
        }
    }
}
