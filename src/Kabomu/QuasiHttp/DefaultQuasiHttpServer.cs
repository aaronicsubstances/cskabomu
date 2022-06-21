using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpServer : IQuasiHttpServer
    {
        private readonly Dictionary<object, ITransferProtocolInternal> _transfers;
        private readonly IParentTransferProtocolInternal _representative;
        private readonly object _lock = new object();
        private bool _running;

        public DefaultQuasiHttpServer()
        {
            _transfers = new Dictionary<object, ITransferProtocolInternal>();
            _representative = new ParentTransferProtocolImpl(this);
        }

        public int DefaultTimeoutMillis { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IEventLoopApi EventLoop { get; set; }

        public async Task Start()
        {
            lock (_lock)
            {
                if (_running)
                {
                    return;
                }
                _running = true;
            }
            await Transport.Start();
            AcceptConnections();
        }

        private async void AcceptConnections()
        {
            while (true)
            {
                try
                {
                    lock (_lock)
                    {
                        if (!_running)
                        {
                            break;
                        }
                    }
                    var connection = await Transport.ReceiveConnection();
                    await Receive(connection);
                }
                catch (Exception e)
                {
                    ErrorHandler?.Invoke(e, "error encountered during receiving");
                }
            }
        }

        public async Task Stop()
        {
            lock (_lock)
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
            var cause = new Exception("reset");

            var tasks = new List<Task>();
            lock (_lock)
            {
                foreach (var transfer in _transfers.Values)
                {
                    tasks.Add(DisableTransfer(transfer, cause));
                }
                _transfers.Clear();
            }

            await Task.WhenAll(tasks);
        }

        private async Task Receive(object connection)
        {
            if (connection == null)
            {
                throw new ArgumentException("null connection");
            }

            ReceiveProtocolInternal transfer;
            Task timeoutTask, workTask;
            lock (_lock)
            {
                transfer = new ReceiveProtocolInternal(_lock)
                {
                    Parent = _representative,
                    Connection = connection,
                    TimeoutMillis = DefaultTimeoutMillis
                };
                _transfers.Add(connection, transfer);
                timeoutTask = SetResponseTimeout(transfer);
                workTask = transfer.Receive();
            }
            ExceptionDispatchInfo capturedError = null;
            var firstCompletedTask = await Task.WhenAny(timeoutTask, workTask);
            try
            {
                await firstCompletedTask;
            }
            catch (Exception e)
            {
                capturedError = ExceptionDispatchInfo.Capture(e);
            }

            Task abortTask = null;
            lock (_lock)
            {
                if (transfer.IsAborted)
                {
                    return;
                }
                if (capturedError != null)
                {
                    abortTask = AbortTransfer(transfer, capturedError.SourceException);
                }
            }
            if (abortTask != null)
            {
                await abortTask;
            }
            capturedError?.Throw();
            await workTask;
        }

        private Task SetResponseTimeout(ITransferProtocolInternal transfer)
        {
            transfer.TimeoutCancellationHandle = new CancellationTokenSource();
            return EventLoop.SetTimeout<Task>(transfer.TimeoutMillis, transfer.TimeoutCancellationHandle.Token, () =>
                throw new Exception("receive timeout"));
        }

        private async Task AbortTransfer(ITransferProtocolInternal transfer, Exception e)
        {
            if (transfer.IsAborted)
            {
                return;
            }
            _transfers.Remove(transfer.Connection);
            await DisableTransfer(transfer, e);
        }

        private async Task DisableTransfer(ITransferProtocolInternal transfer, Exception e)
        {
            await transfer.Cancel(e);

            Task releaseTask = null;
            lock (_lock)
            {
                transfer.TimeoutCancellationHandle?.Cancel();
                transfer.IsAborted = true;

                if (transfer.Connection != null)
                {
                    releaseTask = Transport.ReleaseConnection(transfer.Connection, true);
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

            public int DefaultTimeoutMillis => _delegate.DefaultTimeoutMillis;

            public IQuasiHttpApplication Application => _delegate.Application;

            public IQuasiHttpTransport Transport => _delegate.Transport;

            public Task AbortTransfer(ITransferProtocolInternal transfer, Exception e)
            {
                return _delegate.AbortTransfer(transfer, e);
            }
        }
    }
}
