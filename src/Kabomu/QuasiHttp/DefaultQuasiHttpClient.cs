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
        private readonly Func<SendTransferInternal, Exception, Task> AbortTransferCallback;

        public DefaultQuasiHttpClient()
        {
            AbortTransferCallback = AbortTransfer;
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

            Task<IQuasiHttpResponse> workTask;
            using (await MutexApi.Synchronize())
            {
                _transfers.Add(transfer);
                
                int transferTimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                    options, DefaultSendOptions, 0);
                SetResponseTimeout(transfer, transferTimeoutMillis);

                var extraConnectivityParams = ProtocolUtilsInternal.DetermineEffectiveConnectivityParams(
                    options, DefaultSendOptions);
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
                        options, DefaultSendOptions, 0, TransportUtils.DefaultMaxChunkSize);
                    workTask = AllocateConnectionAndSend(connectivityParams, transfer, request,
                        protocolMaxChunkSize);
                }
            }

            var firstCompletedTask = await Task.WhenAny(transfer.CancellationTcs.Task, workTask);
            try
            {
                return await firstCompletedTask;
            }
            catch (Exception e)
            {
                await AbortTransfer(transfer, e);
                throw;
            }
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestDirectly(
            IConnectivityParams connectivityParams,
            SendTransferInternal transfer, IQuasiHttpRequest request)
        {
            var transportBypass = TransportBypass;
            if (transportBypass == null)
            {
                throw new MissingDependencyException("transport bypass");
            }
            IQuasiHttpResponse res = await transportBypass.ProcessSendRequest(request, connectivityParams);

            await AbortTransfer(transfer, null);

            if (res == null)
            {
                throw new Exception("no response");
            }

            return res;
        }

        private async Task<IQuasiHttpResponse> AllocateConnectionAndSend(
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

            Task<IQuasiHttpResponse> resTask;
            using (await MutexApi.Synchronize())
            {
                if (transfer.IsAborted)
                {
                    return null;
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
            TimerApi?.ClearTimeout(transfer.TimeoutId);
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
