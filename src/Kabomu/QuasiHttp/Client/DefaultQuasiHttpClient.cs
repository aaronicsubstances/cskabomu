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

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// The standard implementation of the client side of the quasi http protocol defined by the Kabomu library.
    /// </summary>
    /// <remarks>
    /// This class implements the <see cref="IQuasiHttpClient"/> interface in order to provide
    /// the client facing side of networking for end users. It is the complement to the 
    /// <see cref="Server.DefaultQuasiHttpServer"/> class for supporting the semantics of HTTP client libraries
    /// whiles enabling underlying transport options beyond TCP.
    /// </remarks>
    public class DefaultQuasiHttpClient : IQuasiHttpClient
    {
        private readonly Random _randGen = new Random();
        private readonly ISet<SendTransferInternal> _transfers = new HashSet<SendTransferInternal>();
        private readonly Func<object, Exception, IQuasiHttpResponse, Task> AbortTransferCallback;
        private readonly Func<object, IQuasiHttpResponse, Task> AbortTransferCallback2;

        /// <summary>
        /// Creates a new instance of the <see cref="DefaultQuasiHttpClient"/> class with defaults provided
        /// for the <see cref="MutexApi"/> and <see cref="TimerApi"/> properties.
        /// </summary>
        public DefaultQuasiHttpClient()
        {
            AbortTransferCallback = CancelSend;
            AbortTransferCallback2 = CancelSend;
            MutexApi = new LockBasedMutexApi();
            TimerApi = new DefaultTimerApi();
        }

        public IQuasiHttpSendOptions DefaultSendOptions { get; set; }
        public IQuasiHttpClientTransport Transport { get; set; }
        public IQuasiHttpAltTransport TransportBypass { get; set; }
        public double TransportBypassProbabilty { get; set; }
        public double ResponseStreamingProbabilty { get; set; }

        /// <summary>
        /// Gets or sets mutex api used to guard multithreaded 
        /// access to connection allocation operations of this class.
        /// </summary>
        /// <remarks> 
        /// An ordinary lock object is the initial value for this property, and so there is no need to modify
        /// this property except for advanced scenarios.
        /// </remarks>
        public IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Gets or sets timer api used to generate timeouts in this class.
        /// </summary>
        /// <remarks> 
        /// An instance of <see cref="DefaultTimerApi"/> class is the initial value for this property,
        /// and so there is no need to modify this property except for advanced scenarios.
        /// </remarks>
        public ITimerApi TimerApi { get; set; }

        /// <summary>
        /// Cancels a send request if it is still ongoing. Invalid cancellation handles are simply ignored.
        /// </summary>
        /// <param name="sendCancellationHandle">cancellation handle received from <see cref="Send2"/></param>
        public void CancelSend(object sendCancellationHandle)
        {
            if (sendCancellationHandle is SendTransferInternal transfer)
            {
                _ = AbortTransfer(transfer, new Exception("send cancelled"), null);
            }
        }

        private Task CancelSend(object transferObj, IQuasiHttpResponse res)
        {
            var transfer = (SendTransferInternal)transferObj;
            return AbortTransfer(transfer, null, res);
        }

        private Task CancelSend(object transferObj, Exception cancellationError,
            IQuasiHttpResponse res)
        {
            var transfer = (SendTransferInternal)transferObj;
            return AbortTransfer(transfer, cancellationError, res);
        }

        public Tuple<Task<IQuasiHttpResponse>, object> Send2(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            var transfer = new SendTransferInternal
            {
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint
                },
                Request = request,
                SendOptions = options,
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            var sendTask = ProcessSend(transfer);
            return Tuple.Create(sendTask, (object)transfer);
        }

        public Task<IQuasiHttpResponse> Send(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            var transfer = new SendTransferInternal
            {
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint
                },
                Request = request,
                SendOptions = options,
                CancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously)
            };
            return ProcessSend(transfer);
        }

        private async Task<IQuasiHttpResponse> ProcessSend(SendTransferInternal transfer)
        {
            Task workTask;
            using (await MutexApi.Synchronize())
            {
                // NB: negative value is allowed for timeout, which indicates infinite timeout.
                transfer.TimeoutMillis = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                    transfer.SendOptions?.TimeoutMillis,
                    DefaultSendOptions?.TimeoutMillis,
                    0);
                SetSendTimeout(transfer);

                _transfers.Add(transfer);

                transfer.ConnectivityParams.ExtraParams = ProtocolUtilsInternal.DetermineEffectiveOptions(
                    transfer.SendOptions?.ExtraConnectivityParams,
                    DefaultSendOptions?.ExtraConnectivityParams);

                transfer.ResponseStreamingEnabled = ProtocolUtilsInternal.DetermineEffectiveBooleanOption(
                    transfer.SendOptions?.ResponseStreamingEnabled,
                    DefaultSendOptions?.ResponseStreamingEnabled,
                    _randGen.NextDouble() < ResponseStreamingProbabilty);

                transfer.MaxChunkSize = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    transfer.SendOptions?.MaxChunkSize,
                    DefaultSendOptions?.MaxChunkSize,
                    TransportUtils.DefaultMaxChunkSize);

                transfer.ResponseBodyBufferingSizeLimit = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(
                    transfer.SendOptions?.ResponseBodyBufferingSizeLimit,
                    DefaultSendOptions?.ResponseBodyBufferingSizeLimit,
                    TransportUtils.DefaultResponseBodyBufferingSizeLimit);

                if (TransportBypass != null && (Transport == null || _randGen.NextDouble() < TransportBypassProbabilty))
                {
                    workTask = ProcessSendRequestDirectly(transfer);
                }
                else
                {
                    workTask = AllocateConnectionAndSend(transfer);
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

        private async Task ProcessSendRequestDirectly(SendTransferInternal transfer)
        {
            var protocol = new AltSendProtocolInternal
            {
                Parent = transfer,
                TransportBypass = TransportBypass,
                AbortCallback = AbortTransferCallback2,
                ConnectivityParams = transfer.ConnectivityParams,
                ResponseStreamingEnabled = transfer.ResponseStreamingEnabled,
                ResponseBodyBufferingSizeLimit = transfer.ResponseBodyBufferingSizeLimit,
                MaxChunkSize = transfer.MaxChunkSize,
            };
            transfer.Protocol = protocol;
            await protocol.Send(transfer.Request);
        }

        private async Task AllocateConnectionAndSend(SendTransferInternal transfer)
        {
            var transport = Transport;
            if (transport == null)
            {
                throw new MissingDependencyException("transport");
            }

            var connectionResponse = await transport.AllocateConnection(transfer.ConnectivityParams);

            Task resTask;
            using (await MutexApi.Synchronize())
            {
                if (connectionResponse?.Connection == null)
                {
                    throw new Exception("no connection created");
                }

                if (transfer.IsAborted)
                {
                    // Oops...connection established took so long, or a reset happened.
                    // just release the connection.
                    resTask = transport.ReleaseConnection(connectionResponse.Connection);
                }
                else
                {
                    var protocol = new DefaultSendProtocolInternal
                    {
                        Parent = transfer,
                        Transport = transport,
                        Connection = connectionResponse.Connection,
                        ResponseStreamingEnabled = transfer.ResponseStreamingEnabled,
                        ResponseBodyBufferingSizeLimit = transfer.ResponseBodyBufferingSizeLimit,
                        MaxChunkSize = transfer.MaxChunkSize,
                        AbortCallback = AbortTransferCallback
                    };
                    transfer.Protocol = protocol;

                    resTask = protocol.Send(transfer.Request);
                }
            }

            await resTask;
        }

        public async Task Reset(Exception cause)
        {
            var cancellationException = cause ?? new Exception("client reset");

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

        private void SetSendTimeout(SendTransferInternal transfer)
        {
            if (transfer.TimeoutMillis <= 0)
            {
                return;
            }
            var timer = TimerApi;
            if (timer == null)
            {
                throw new MissingDependencyException("timer api");
            }
            transfer.TimeoutId = timer.WhenSetTimeout(async () =>
            {
                await AbortTransfer(transfer, new Exception("send timeout"), null);
            }, transfer.TimeoutMillis).Item2;
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

        private async Task DisableTransfer(SendTransferInternal transfer, Exception cancellationError,
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
            bool cancelProtocol = false;
            if (cancellationError != null || res?.Body == null || !transfer.ResponseStreamingEnabled)
            {
                cancelProtocol = true;
            }
            Task cancelProtocolTask = null;
            if (cancelProtocol)
            {
                using (await MutexApi.Synchronize())
                {
                    // just in case cancellation was requested even before transfer protocol could
                    // be set up...check to avoid possible null pointer error.
                    cancelProtocolTask = transfer.Protocol?.Cancel();
                }
            }
            if (cancelProtocolTask != null)
            {
                await cancelProtocolTask;
            }
            if (transfer.Request.Body != null)
            {
                try
                {
                    await transfer.Request.Body.EndRead();
                }
                catch (Exception) { }
            }
        }
    }
}
