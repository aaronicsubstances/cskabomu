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
        private readonly Random _randGen = new Random();
        private readonly ISet<SendTransferInternal> _transfers = new HashSet<SendTransferInternal>();
        private readonly Func<SendTransferInternal, Exception, Task> AbortTransferCallback;

        public DefaultQuasiHttpClient()
        {
            AbortTransferCallback = AbortTransfer;
            MutexApi = new LockBasedMutexApi();
            MutexApiFactory = null;
            EventLoopApi = new UnsynchronizedEventLoopApi();
        }

        public IQuasiHttpSendOptions DefaultSendOptions { get; set; }
        public IQuasiHttpClientTransport Transport { get; set; }
        public IQuasiHttpTransportBypass TransportBypass { get; set; }
        public double TransportBypassProbabilty { get; set; }
        public IMutexApi MutexApi { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }
        public IEventLoopApi EventLoopApi { get; set; }

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
                TransferCancellationHandle = new CancellationTokenSource(),
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };

            Task<IMutexApi> transferMutexTask;
            using (await MutexApi.Synchronize())
            {
                transferMutexTask = ProtocolUtilsInternal.DetermineEffectiveMutexApi(
                    options?.ProcessingMutexApi, MutexApiFactory, null);
            }

            var transferMutex = await transferMutexTask;

            Task<IQuasiHttpResponse> workTask;
            using (await MutexApi.Synchronize())
            {
                _transfers.Add(transfer);
                
                int transferTimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                    options, DefaultSendOptions, 0);
                SetResponseTimeout(transfer, transferTimeoutMillis);

                var requestEnvironment = ProtocolUtilsInternal.DetermineEffectiveRequestEnvironment(
                    options, DefaultSendOptions);
                var connectionAllocationRequest = new DefaultConnectionAllocationRequest
                {
                    RemoteEndpoint = remoteEndpoint,
                    Environment = requestEnvironment,
                    ProcessingMutexApi = transferMutex
                };
                if (TransportBypass != null && (Transport == null || _randGen.NextDouble() < TransportBypassProbabilty))
                {
                    workTask = ProcessSendRequestDirectly(connectionAllocationRequest, transfer, request);
                }
                else
                {
                    int protocolMaxChunkSize = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(
                        options, DefaultSendOptions, 0, TransportUtils.DefaultMaxChunkSize);
                    workTask = AllocateConnectionAndSend(connectionAllocationRequest, transfer, request,
                        protocolMaxChunkSize);
                }
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
            return await firstCompletedTask;
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestDirectly(
            IConnectionAllocationRequest connectionAllocationRequest,
            SendTransferInternal transfer, IQuasiHttpRequest request)
        {
            IQuasiHttpResponse res = await TransportBypass.ProcessSendRequest(request, connectionAllocationRequest);

            Task abortTask;
            using (await MutexApi.Synchronize())
            {
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
            SendTransferInternal transfer, IQuasiHttpRequest request, int protocolMaxChunkSize)
        {
            object connection = await Transport.AllocateConnection(connectionAllocationRequest);

            Task<IQuasiHttpResponse> resTask;
            using (await MutexApi.Synchronize())
            {
                if (transfer.IsAborted)
                {
                    return null;
                }

                if (connection == null)
                {
                    throw new Exception("no connection created");
                }

                transfer.Protocol = new SendProtocolInternal
                {
                    Parent = transfer,
                    Transport = Transport,
                    Connection = connection,
                    MutexApi = connectionAllocationRequest.ProcessingMutexApi,
                    MaxChunkSize = protocolMaxChunkSize,
                    AbortCallback = AbortTransferCallback
                };

                resTask = transfer.Protocol.Send(request);
            }

            return await resTask;
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

        private void SetResponseTimeout(SendTransferInternal transfer, int transferTimeoutMillis)
        {
            var ev = EventLoopApi;
            if (ev == null || transferTimeoutMillis <= 0)
            {
                return;
            }
            transfer.TimeoutId = ev.SetTimeout(transferTimeoutMillis, async () =>
            {
                await AbortTransfer(transfer, new Exception("send timeout"));
            }).Item2;
        }

        private async Task AbortTransfer(SendTransferInternal transfer, Exception cancellationError)
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

        private async Task DisableTransfer(SendTransferInternal transfer, Exception cancellationError)
        {
            transfer.TransferCancellationHandle.Cancel();
            EventLoopApi?.ClearTimeout(transfer.TimeoutId);
            transfer.IsAborted = true;
            if (cancellationError != null)
            {
                transfer.CancellationTcs.SetException(cancellationError);
            }
            if (transfer.Protocol != null)
            {
                await transfer.Protocol.Cancel();
            }
        }
    }
}
