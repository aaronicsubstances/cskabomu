using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public async Task<IQuasiHttpResponse> SendAsync(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            return await ProcessSendAsync(remoteEndpoint, request, options);
        }

        private async Task<IQuasiHttpResponse> ProcessSendAsync(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            var transfer = new SendProtocolInternal
            {
                Parent = _representative,
                SendCallback = new TaskCompletionSource<IQuasiHttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
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
            var timeoutTask = SetResponseTimeoutAsync(transfer, true);
            Task<IQuasiHttpResponse> workTask;
            if (Transport.DirectSendRequestProcessingEnabled)
            {
                workTask = ProcessSendRequestDirectlyAsync(remoteEndpoint, transfer, request);
            }
            else
            {
                workTask = AllocateConnectionAsync(remoteEndpoint, transfer, request);
            }
            var firstCompletedTask = await Task.WhenAny(transfer.SendCallback.Task, timeoutTask, workTask);
            await firstCompletedTask;
            return await transfer.SendCallback.Task;
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestDirectlyAsync(object remoteEndpoint,
            ITransferProtocolInternal transfer, IQuasiHttpRequest request)
        {
            IQuasiHttpResponse res = null;
            Exception readError = null;
            try
            {
                res = await EventLoop.MutexWrap(Transport.ProcessSendRequestAsync(remoteEndpoint, request));
            }
            catch (Exception e)
            {
                readError = e;
            }

            if (transfer.IsAborted)
            {
                return null;
            }

            if (readError != null)
            {
                return await AbortTransferAsync(transfer, readError);
            }

            if (res == null)
            {
                return await AbortTransferAsync(transfer, new Exception("no response"));
            }

            transfer.SendCallback.SetResult(res);
            return await AbortTransferAsync(transfer, null);
        }

        private async Task<IQuasiHttpResponse> AllocateConnectionAsync(object remoteEndpoint,
            ITransferProtocolInternal transfer, IQuasiHttpRequest request)
        {
            object connection = null;
            Exception connectError = null;
            try
            {
                connection = EventLoop.MutexWrap(Transport.AllocateConnectionAsync(remoteEndpoint));
            }
            catch (Exception e)
            {
                connectError = e;
            }

            if (transfer.IsAborted)
            {
                return null;
            }

            if (connectError != null)
            {
                return await AbortTransferAsync(transfer, connectError);
            }

            if (connection == null)
            {
                return await AbortTransferAsync(transfer, new Exception("no connection created"));
            }

            transfer.Connection = connection;
            _transfersWithConnections.Add(connection, transfer);
            _transfersWithoutConnections.Remove(transfer);
            return await transfer.SendAsync(request);
        }

        public async Task ReceiveAsync(object connection)
        {
            if (connection == null)
            {
                throw new ArgumentException("null connection");
            }

            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            var transfer = new ReceiveProtocolInternal
            {
                Parent = _representative,
                Connection = connection,
                TimeoutMillis = DefaultTimeoutMillis
            };
            _transfersWithConnections.Add(connection, transfer);
            var timeoutTask = SetResponseTimeoutAsync(transfer, false);
            var workTask = transfer.ReceiveAsync();
            var firstCompletedTask = await Task.WhenAny(timeoutTask, workTask);
            await firstCompletedTask;
        }

        public async Task ResetAsync(Exception cause)
        {
            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            cause = cause ?? new Exception("reset");

            foreach (var transfer in _transfersWithConnections.Values)
            {
                try
                {
                    await EventLoop.MutexWrap(DisableTransferAsync(transfer, cause));
                }
                catch (Exception) { }
            }
            foreach (var transfer in _transfersWithoutConnections)
            {
                try
                {
                    await EventLoop.MutexWrap(DisableTransferAsync(transfer, cause));
                }
                catch (Exception) { }
            }
            _transfersWithConnections.Clear();
            _transfersWithoutConnections.Clear();
        }

        private async Task<IQuasiHttpResponse> SetResponseTimeoutAsync(ITransferProtocolInternal transfer, bool forSend)
        {
            transfer.TimeoutCancellationHandle = new CancellationTokenSource();
            await EventLoop.SetTimeoutAsync(transfer.TimeoutMillis, transfer.TimeoutCancellationHandle.Token);
            return await AbortTransferAsync(transfer, new Exception((forSend ? "send" : "receive") + " timeout"));
        }

        private async Task<IQuasiHttpResponse> AbortTransferAsync(ITransferProtocolInternal transfer, Exception e)
        {
            if (transfer.IsAborted)
            {
                return null;
            }
            _transfersWithoutConnections.Remove(transfer);
            if (transfer.Connection != null)
            {
                _transfersWithConnections.Remove(transfer.Connection);
            }
            await EventLoop.MutexWrap(DisableTransferAsync(transfer, e));
            if (transfer.SendCallback != null)
            {
                return await transfer.SendCallback.Task;
            }
            else
            {
                return null;
            }
        }

        private async Task DisableTransferAsync(ITransferProtocolInternal transfer, Exception e)
        {
            await EventLoop.MutexWrap(transfer.Cancel(e));
            transfer.TimeoutCancellationHandle?.Cancel();
            transfer.IsAborted = true;

            if (e != null)
            {
                transfer.SendCallback?.SetException(e);
            }

            try
            {
                if (transfer.Connection != null)
                {
                    await EventLoop.MutexWrap(Transport.ReleaseConnectionAsync(transfer.Connection));
                }
            }
            catch { }

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "transfer error");
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
                return _delegate.AbortTransferAsync(transfer, e);
            }
        }
    }
}
