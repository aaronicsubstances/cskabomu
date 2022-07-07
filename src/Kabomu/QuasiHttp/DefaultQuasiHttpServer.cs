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
        private readonly Dictionary<object, ITransferProtocolInternal> _transfers;
        private readonly IParentTransferProtocolInternal _representative;
        private bool _running;

        public DefaultQuasiHttpServer()
        {
            _transfers = new Dictionary<object, ITransferProtocolInternal>();
            _representative = new ParentTransferProtocolImpl(this);
            MutexApi = new LockBasedMutexApi();
            MutexApiFactory = new LockBasedMutexApiFactory();
        }

        public int OverallReqRespTimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpServerTransport Transport { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public IMutexApi MutexApi { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }

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
            // let TaskScheduler.UnobservedTaskException handle any uncaught task exceptions.
            _ = StartAcceptingConnections();
        }

        private async Task StartAcceptingConnections()
        {
            while (true)
            {
                try
                {
                    Task<IConnectionAllocationResponse> connectTask;
                    using (await MutexApi.Synchronize())
                    {
                        if (!_running)
                        {
                            break;
                        }
                        connectTask = Transport?.ReceiveConnection();
                    }
                    if (connectTask == null)
                    {
                        break;
                    }
                    var connectionAllocationResponse = await connectTask;
                    // let TaskScheduler.UnobservedTaskException handle any uncaught task exceptions.
                    _ = AcceptConnection(connectionAllocationResponse);
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
            // since it is desired to clear all pending transfers under lock,
            // and disabling of transfer is an async transfer, we choose
            // not to await on each disabling, but rather to wait on them
            // after clearing the transfers.
            var tasks = new List<Task>();
            using (await MutexApi.Synchronize())
            {
                foreach (var transfer in _transfers.Values)
                {
                    tasks.Add(DisableTransfer(transfer));
                }
                _transfers.Clear();
            }

            await Task.WhenAll(tasks);
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

            IMutexApi transferMutex = await ProtocolUtilsInternal.DetermineEffectiveMutexApi(
                connectionAllocationResponse.ProcessingMutexApi, MutexApiFactory, MutexApi);

            ReceiveProtocolInternal transfer;
            Task timeoutTask, workTask;
            using (await MutexApi.Synchronize())
            {
                transfer = new ReceiveProtocolInternal
                {
                    Parent = _representative,
                    Application = Application,
                    Transport = Transport,
                    Connection = connectionAllocationResponse.Connection,
                    RequestEnvironment = connectionAllocationResponse.Environment,
                    MutexApi = transferMutex
                };
                transfer.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(
                    null, null, MaxChunkSize, TransportUtils.DefaultMaxChunkSize);
                _transfers.Add(transfer.Connection, transfer);
                var transferTimeoutMillis = OverallReqRespTimeoutMillis;
                timeoutTask = ProtocolUtilsInternal.SetResponseTimeout(EventLoop, transfer, transferTimeoutMillis,
                    "receive timeout");
                workTask = transfer.Receive();
            }
            ExceptionDispatchInfo capturedError = null;
            Task firstCompletedTask;
            if (timeoutTask != null)
            {
                firstCompletedTask = await Task.WhenAny(timeoutTask, workTask);
            }
            else
            {
                firstCompletedTask = workTask;
            }
            try
            {
                await firstCompletedTask;
            }
            catch (Exception e)
            {
                capturedError = ExceptionDispatchInfo.Capture(e);
            }

            Task abortTask = null;
            using (await MutexApi.Synchronize())
            {
                if (capturedError != null)
                {
                    abortTask = AbortTransfer(transfer);
                }
            }
            if (abortTask != null)
            {
                await abortTask;
            }
            capturedError?.Throw();
            await workTask;
        }

        private async Task AbortTransfer(ITransferProtocolInternal transfer)
        {
            if (transfer.IsAborted)
            {
                return;
            }
            _transfers.Remove(transfer.Connection);
            await DisableTransfer(transfer);
        }

        private async Task DisableTransfer(ITransferProtocolInternal transfer)
        {
            await transfer.Cancel();

            Task releaseTask = null;
            using (await MutexApi.Synchronize())
            {
                transfer.TimeoutCancellationHandle?.Cancel();
                transfer.IsAborted = true;

                if (transfer.Connection != null)
                {
                    releaseTask = Transport.ReleaseConnection(transfer.Connection);
                }
            }
            if (releaseTask != null)
            {
                await releaseTask;
            }
        }

        private class ParentTransferProtocolImpl : IParentTransferProtocolInternal
        {
            private readonly DefaultQuasiHttpServer _delegate;

            public ParentTransferProtocolImpl(DefaultQuasiHttpServer passThrough)
            {
                _delegate = passThrough;
            }

            public Task AbortTransfer(ITransferProtocolInternal transfer)
            {
                return _delegate.AbortTransfer(transfer);
            }
        }
    }
}
