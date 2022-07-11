using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        private readonly Func<ReceiveTransferInternal, Exception, Task> AbortTransferCallback;

        public DefaultQuasiHttpServer()
        {
            AbortTransferCallback = AbortTransfer;
            _transfers = new HashSet<ReceiveTransferInternal>();
            MutexApi = new LockBasedMutexApi();
            MutexApiFactory = null;
            EventLoopApi = new UnsynchronizedEventLoopApi();
        }

        public int OverallReqRespTimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpServerTransport Transport { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IMutexApi MutexApi { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }
        public IEventLoopApi EventLoopApi { get; set; }

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
                        connectTask = Transport.ReceiveConnection();
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
                if (ErrorHandler == null)
                {
                    throw;
                }
                ErrorHandler?.Invoke(e, "error encountered while accepting connections");
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
                if (ErrorHandler == null)
                {
                    throw;
                }
                ErrorHandler?.Invoke(e, "error encountered while receiving a connection");
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
                TransferCancellationHandle = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };

            IMutexApi transferMutex = await ProtocolUtilsInternal.DetermineEffectiveMutexApi(
                connectionAllocationResponse.ProcessingMutexApi, MutexApiFactory, null);

            Task workTask;
            using (await MutexApi.Synchronize())
            {
                _transfers.Add(transfer);
                SetResponseTimeout(transfer, OverallReqRespTimeoutMillis);

                var effectiveMaxChunkSize = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(
                    null, null, MaxChunkSize, TransportUtils.DefaultMaxChunkSize);
                transfer.Protocol = new ReceiveProtocolInternal
                {
                    Parent = transfer,
                    AbortCallback = AbortTransferCallback,
                    MaxChunkSize = effectiveMaxChunkSize,
                    Application = Application,
                    Transport = Transport,
                    Connection = connectionAllocationResponse.Connection,
                    RequestEnvironment = connectionAllocationResponse.Environment,
                    MutexApi = transferMutex
                };
                workTask = transfer.Protocol.Receive();
            }
            var firstCompletedTask = await Task.WhenAny(transfer.CancellationTcs.Task, workTask);
            ExceptionDispatchInfo capturedError = null;
            try
            {
                await firstCompletedTask;
            }
            catch (Exception e)
            {
                capturedError = ExceptionDispatchInfo.Capture(e);
            }

            Task abortTask = null;
            if (capturedError != null)
            {
                using (await MutexApi.Synchronize())
                {
                    abortTask = AbortTransfer(transfer, capturedError.SourceException);
                }
            }
            if (abortTask != null)
            {
                await abortTask;
            }
            capturedError?.Throw();
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
                        tasks.Add(DisableTransfer(transfer, cancellationException));
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
            var ev = EventLoopApi;
            if (ev == null)
            {
                throw new MissingDependencyException("event loop");
            }
            transfer.TimeoutId = ev.SetTimeout(transferTimeoutMillis, async () =>
            {
                await AbortTransfer(transfer, new Exception("receive timeout"));
            }).Item2;
        }

        private async Task AbortTransfer(ReceiveTransferInternal transfer, Exception cancellationError)
        {
            Task disableTransferTask;
            using (await MutexApi.Synchronize())
            {
                if (transfer.IsAborted)
                {
                    return;
                }
                _transfers.Remove(transfer);
                disableTransferTask = DisableTransfer(transfer, cancellationError);
            }
            await disableTransferTask;
        }

        private async Task DisableTransfer(ReceiveTransferInternal transfer, Exception cancellationError)
        {
            transfer.TransferCancellationHandle.Cancel();
            EventLoopApi?.ClearTimeout(transfer.TimeoutId);
            transfer.IsAborted = true;
            if (cancellationError != null)
            {
                transfer.CancellationTcs.SetException(cancellationError);
            }
            await transfer.Protocol.Cancel();
        }
    }
}
