using Kabomu.Common;
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
    public class KabomuQuasiHttpClient : IQuasiHttpClient
    {
        private readonly Dictionary<object, ITransferProtocolInternal> _transfersWithConnections;
        private readonly HashSet<ITransferProtocolInternal> _transfersWithoutConnections;
        private readonly IParentTransferProtocolInternal _representative;

        public KabomuQuasiHttpClient()
        {
            _transfersWithConnections = new Dictionary<object, ITransferProtocolInternal>();
            _transfersWithoutConnections = new HashSet<ITransferProtocolInternal>();
            _representative = new ParentTransferProtocolImpl(this);
        }

        public int DefaultTimeoutMillis { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public IEventLoopApi EventLoop { get; set; }

        public Task<IQuasiHttpResponse> Send(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            return ProcessSend(remoteEndpoint, request, options);
        }

        private async Task<IQuasiHttpResponse> ProcessSend(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            Task<IQuasiHttpResponse> workTask;
            Task timeoutTask;
            SendProtocolInternal transfer;
            lock (EventLoop)
            {
                transfer = new SendProtocolInternal
                {
                    Parent = _representative
                };
                if (options != null)
                {
                    transfer.TimeoutMillis = options.TimeoutMillis;
                }
                if (transfer.TimeoutMillis <= 0)
                {
                    transfer.TimeoutMillis = DefaultTimeoutMillis;
                }
                _transfersWithoutConnections.Add(transfer);
                timeoutTask = SetResponseTimeout(transfer, true);
                if (Transport.DirectSendRequestProcessingEnabled)
                {
                    workTask = ProcessSendRequestDirectly(remoteEndpoint, transfer, request);
                }
                else
                {
                    workTask = AllocateConnection(remoteEndpoint, transfer, request);
                }
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
            lock (EventLoop)
            {
                if (transfer.IsAborted)
                {
                    return null;
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
            return await workTask;
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestDirectly(object remoteEndpoint,
            ITransferProtocolInternal transfer, IQuasiHttpRequest request)
        {
            IQuasiHttpResponse res = await Transport.ProcessSendRequest(remoteEndpoint, request);

            Task abortTask;
            lock (EventLoop)
            {
                if (transfer.IsAborted)
                {
                    return null;
                }
                abortTask = AbortTransfer(transfer, null);
            }

            await abortTask;

            if (res == null)
            {
                throw new Exception("no response");
            }

            return res;
        }

        private async Task<IQuasiHttpResponse> AllocateConnection(object remoteEndpoint,
            SendProtocolInternal transfer, IQuasiHttpRequest request)
        {
            object connection = await Transport.AllocateConnection(remoteEndpoint);

            lock (EventLoop)
            {
                if (transfer.IsAborted)
                {
                    return null;
                }

                if (connection == null)
                {
                    throw new Exception("no connection created");
                }

                transfer.Connection = connection;
                _transfersWithConnections.Add(connection, transfer);
                _transfersWithoutConnections.Remove(transfer);
            }

            return await transfer.Send(request);
        }

        public async Task Receive(object connection)
        {
            if (connection == null)
            {
                throw new ArgumentException("null connection");
            }

            ReceiveProtocolInternal transfer;
            Task timeoutTask, workTask;
            lock (EventLoop)
            {
                transfer = new ReceiveProtocolInternal
                {
                    Parent = _representative,
                    Connection = connection,
                    TimeoutMillis = DefaultTimeoutMillis
                };
                _transfersWithConnections.Add(connection, transfer);
                timeoutTask = SetResponseTimeout(transfer, false);
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
            lock (EventLoop)
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

        public async Task Reset(Exception cause)
        {
            cause = cause ?? new Exception("reset");

            var tasks = new List<Task>();
            lock (EventLoop)
            {
                foreach (var transfer in _transfersWithConnections.Values)
                {
                    tasks.Add(DisableTransfer(transfer, cause));
                }
                foreach (var transfer in _transfersWithoutConnections)
                {
                    tasks.Add(DisableTransfer(transfer, cause));
                }
                _transfersWithConnections.Clear();
                _transfersWithoutConnections.Clear();
            }

            await Task.WhenAll(tasks);
        }

        private Task SetResponseTimeout(ITransferProtocolInternal transfer, bool forSend)
        {
            transfer.TimeoutCancellationHandle = new CancellationTokenSource();
            return EventLoop.SetTimeout<Task>(transfer.TimeoutMillis, transfer.TimeoutCancellationHandle.Token, () =>
                throw new Exception((forSend ? "send" : "receive") + " timeout"));
        }

        private async Task AbortTransfer(ITransferProtocolInternal transfer, Exception e)
        {
            if (transfer.IsAborted)
            {
                return;
            }
            _transfersWithoutConnections.Remove(transfer);
            if (transfer.Connection != null)
            {
                _transfersWithConnections.Remove(transfer.Connection);
            }
            await DisableTransfer(transfer, e);
        }

        private async Task DisableTransfer(ITransferProtocolInternal transfer, Exception e)
        {
            await transfer.Cancel(e);

            Task releaseTask = null;
            lock (EventLoop)
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
            private readonly KabomuQuasiHttpClient _delegate;

            public ParentTransferProtocolImpl(KabomuQuasiHttpClient passThrough)
            {
                _delegate = passThrough;
            }

            public int DefaultTimeoutMillis => _delegate.DefaultTimeoutMillis;

            public IQuasiHttpApplication Application => _delegate.Application;

            public IQuasiHttpTransport Transport => _delegate.Transport;

            public IEventLoopApi EventLoop => _delegate.EventLoop;

            public Task AbortTransfer(ITransferProtocolInternal transfer, Exception e)
            {
                return _delegate.AbortTransfer(transfer, e);
            }
        }
    }
}
