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
    public class DefaultQuasiHttpClient : IQuasiHttpClient
    {
        private readonly Dictionary<object, ITransferProtocolInternal> _transfersWithConnections;
        private readonly ISet<ITransferProtocolInternal> _transfersWithoutConnections;
        private readonly IParentTransferProtocolInternal _representative;
        private readonly object _lock = new object();

        public DefaultQuasiHttpClient()
        {
            _transfersWithConnections = new Dictionary<object, ITransferProtocolInternal>();
            _transfersWithoutConnections = new HashSet<ITransferProtocolInternal>();
            _representative = new ParentTransferProtocolImpl(this);
        }

        public IQuasiHttpSendOptions DefaultSendOptions { get; set; }
        public IQuasiHttpClientTransport Transport { get; set; }
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
            Task timeoutTask = null;
            SendProtocolInternal transfer;
            lock (_lock)
            {
                transfer = new SendProtocolInternal(_lock)
                {
                    Parent = _representative
                };
                transfer.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(
                    options, DefaultSendOptions, 0, TransportUtils.DefaultMaxChunkSize);
                int transferTimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                    options, DefaultSendOptions, 0);
                var requestEnvironment = ProtocolUtilsInternal.DetermineEffectiveRequestEnvironment(
                    options, DefaultSendOptions);
                _transfersWithoutConnections.Add(transfer);
                if (transferTimeoutMillis > 0)
                {
                    timeoutTask = SetResponseTimeout(transfer, transferTimeoutMillis);
                }
                var connectionAllocationRequest = new DefaultConnectionAllocationRequest
                {
                    RemoteEndpoint = remoteEndpoint,
                    Environment = requestEnvironment
                };
                if (Transport.DirectSendRequestProcessingEnabled)
                {
                    workTask = ProcessSendRequestDirectly(connectionAllocationRequest, transfer, request);
                }
                else
                {
                    workTask = AllocateConnectionAndSend(connectionAllocationRequest, transfer, request);
                }
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
            lock (_lock)
            {
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

        private async Task<IQuasiHttpResponse> ProcessSendRequestDirectly(
            IConnectionAllocationRequest connectionAllocationRequest,
            ITransferProtocolInternal transfer, IQuasiHttpRequest request)
        {
            IQuasiHttpResponse res = await Transport.ProcessSendRequest(request, connectionAllocationRequest);

            Task abortTask;
            lock (_lock)
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

        private async Task<IQuasiHttpResponse> AllocateConnectionAndSend(
            IConnectionAllocationRequest connectionAllocationRequest,
            SendProtocolInternal transfer, IQuasiHttpRequest request)
        {
            object connection = await Transport.AllocateConnection(connectionAllocationRequest);

            lock (_lock)
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

        public async Task Reset(Exception cause)
        {
            cause = cause ?? new Exception("reset");

            var tasks = new List<Task>();
            lock (_lock)
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

        private Task SetResponseTimeout(ITransferProtocolInternal transfer, int transferTimeoutMillis)
        {
            transfer.TimeoutCancellationHandle = new CancellationTokenSource();
            return EventLoop.SetTimeout(transferTimeoutMillis, transfer.TimeoutCancellationHandle.Token, () =>
                throw new Exception("send timeout"));
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
            lock (_lock)
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
            private readonly DefaultQuasiHttpClient _delegate;

            public ParentTransferProtocolImpl(DefaultQuasiHttpClient passThrough)
            {
                _delegate = passThrough;
            }

            public IQuasiHttpApplication Application => throw new NotImplementedException();

            public IQuasiHttpTransport Transport => _delegate.Transport;

            public Task AbortTransfer(ITransferProtocolInternal transfer, Exception e)
            {
                return _delegate.AbortTransfer(transfer, e);
            }
        }
    }
}
