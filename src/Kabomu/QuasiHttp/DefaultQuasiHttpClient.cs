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
    public class DefaultQuasiHttpClient : IQuasiHttpClient
    {
        private readonly Random _randGen = new Random();
        private readonly ISet<SendTransferInternal> _transfers = new HashSet<SendTransferInternal>();
        private readonly Func<SendTransferInternal, Exception, IQuasiHttpResponse, Task> AbortTransferCallback;
        private readonly Func<SendTransferInternal, IQuasiHttpResponse, Task> PartialAbortTransferCallback;

        public DefaultQuasiHttpClient()
        {
            AbortTransferCallback = AbortTransfer;
            PartialAbortTransferCallback = PartialAbortTransfer;
            MutexApi = new LockBasedMutexApi();
            MutexApiFactory = null;
            TimerApi = new DefaultTimerApi();
        }

        public IQuasiHttpSendOptions DefaultSendOptions { get; set; }
        public IQuasiHttpClientTransport Transport { get; set; }
        public IQuasiHttpTransportBypass TransportBypass { get; set; }
        public double TransportBypassProbabilty { get; set; }
        public IMutexApi MutexApi { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }
        public ITimerApi TimerApi { get; set; }

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
            var transfer = new SendTransferInternal
            {
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };

            Task workTask;
            using (await MutexApi.Synchronize())
            {
                _transfers.Add(transfer);
                
                int transferTimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                    options?.OverallReqRespTimeoutMillis, DefaultSendOptions?.OverallReqRespTimeoutMillis, 0);
                SetResponseTimeout(transfer, transferTimeoutMillis);

                var extraConnectivityParams = ProtocolUtilsInternal.DetermineEffectiveOptions(
                    options?.ConnectivityParams, DefaultSendOptions?.ConnectivityParams);
                var connectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = extraConnectivityParams
                };
                if (TransportBypass != null && (Transport == null || _randGen.NextDouble() < TransportBypassProbabilty))
                {
                    workTask = ProcessSendRequestDirectly(connectivityParams, transfer, request);
                }
                else
                {
                    int protocolMaxChunkSize = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(
                        options?.MaxChunkSize, DefaultSendOptions?.MaxChunkSize, TransportUtils.DefaultMaxChunkSize);
                    workTask = AllocateConnectionAndSend(connectivityParams, transfer, request,
                        protocolMaxChunkSize);
                }
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
            return await transfer.CancellationTcs.Task;
        }

        private async Task ProcessSendRequestDirectly(
            IConnectivityParams connectivityParams,
            SendTransferInternal transfer, IQuasiHttpRequest request)
        {
            var transportBypass = TransportBypass;
            if (transportBypass == null)
            {
                throw new MissingDependencyException("transport bypass");
            }

            var cancellableResTask = transportBypass.ProcessSendRequest(request, connectivityParams);
            transfer.BypassCancellationHandle = cancellableResTask.Item2;

            IQuasiHttpResponse res = await cancellableResTask.Item1;

            if (res == null)
            {
                throw new Exception("no response");
            }

            await AbortTransfer(transfer, null, res);
        }

        private async Task AllocateConnectionAndSend(
            IConnectivityParams connectivityParams,
            SendTransferInternal transfer, IQuasiHttpRequest request, int protocolMaxChunkSize)
        {
            var transport = Transport;
            if (transport == null)
            {
                throw new MissingDependencyException("transport");
            }

            var mutexApiFactory = MutexApiFactory;
            var connectionResponse = await transport.AllocateConnection(connectivityParams);
            var transferMutex = await ProtocolUtilsInternal.DetermineEffectiveMutexApi(
                connectionResponse?.ProcessingMutexApi, mutexApiFactory);

            Task resTask;
            using (await MutexApi.Synchronize())
            {
                if (transfer.IsAborted)
                {
                    return;
                }

                if (connectionResponse?.Connection == null)
                {
                    throw new Exception("no connection created");
                }

                transfer.Protocol = new SendProtocolInternal
                {
                    Parent = transfer,
                    Transport = transport,
                    Connection = connectionResponse.Connection,
                    MutexApi = transferMutex,
                    MaxChunkSize = protocolMaxChunkSize,
                    AbortCallback = AbortTransferCallback,
                    PartialAbortCallback = PartialAbortTransferCallback
                };

                resTask = transfer.Protocol.Send(request);
            }

            await resTask;
        }

        public async Task Reset()
        {
            var cancellationException = new Exception("client reset");

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

        private void SetResponseTimeout(SendTransferInternal transfer, int transferTimeoutMillis)
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
                await AbortTransfer(transfer, new Exception("send timeout"), null);
            }).Item2;
        }

        private async Task AbortTransfer(SendTransferInternal transfer, Exception cancellationError,
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

        private async Task PartialAbortTransfer(SendTransferInternal transfer, IQuasiHttpResponse res)
        {
            using (await MutexApi.Synchronize())
            {
                if (transfer.IsAborted)
                {
                    return;
                }
                transfer.CancellationTcs.TrySetResult(res);
            }
        }

        private async Task DisableTransfer(SendTransferInternal transfer, Exception cancellationError,
            IQuasiHttpResponse res)
        {
            // it is possible task has been completed already, so use TrySet* instead of Set*.
            if (cancellationError != null)
            {
                transfer.CancellationTcs.TrySetException(cancellationError);
            }
            else
            {
                transfer.CancellationTcs.TrySetResult(res);
            }
            transfer.IsAborted = true;
            TimerApi?.ClearTimeout(transfer.TimeoutId);
            if (transfer.Protocol != null)
            {
                await transfer.Protocol.Cancel();
            }
            if (transfer.BypassCancellationHandle != null)
            {
                TransportBypass?.CancelSendRequest(transfer.BypassCancellationHandle);
            }
        }
    }
}
