using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class SendTransferInternal
    {
        public object Mutex { get; set; }
        public ISendProtocolInternal Protocol { get; set; }
        public CancellationTokenSource TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public TaskCompletionSource<ProtocolSendResultInternal> CancellationTcs { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> RequestFunc { get; set; }
        public int MaxChunkSize { get; set; }
        public IConnectivityParams ConnectivityParams { get; set; }
        public object Connection { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        /// <summary>
        /// Assume this call occurs under mutex.
        /// </summary>
        /// <param name="protocolFactory"></param>
        /// <returns></returns>
        public async Task<ProtocolSendResultInternal> StartProtocol(
            Func<SendTransferInternal, ISendProtocolInternal> protocolFactory)
        {
            var protocol = protocolFactory.Invoke(this);
            Protocol = protocol;
            var res = await protocol.Send();
            Task cancelTask = null;
            lock (Mutex)
            {
                if (IsAborted)
                {
                    try
                    {
                        cancelTask = protocol.Cancel();
                    }
                    catch (Exception) { } // ignore
                }
            }
            if (cancelTask != null)
            {
                await cancelTask;
            }
            await Abort(null, res);
            return res;
        }

        public  async Task Abort(Exception cancellationError, ProtocolSendResultInternal res)
        {
            Task disableTask = null;
            var disposeRes = false;
            lock (Mutex)
            {
                if (IsAborted)
                {
                    disposeRes = true;
                }
                else
                {
                    IsAborted = true;
                    disableTask = Disable(cancellationError, res,
                        CancellationTcs, TimeoutId, Protocol, Request);
                }
            }
            if (disposeRes)
            {
                // dispose off response
                try
                {
                    var resDisposeTask = res?.Response?.CustomDispose();
                    if (resDisposeTask != null)
                    {
                        await resDisposeTask;
                    }
                }
                catch (Exception) { } // ignore.
            }
            if (disableTask != null)
            {
                await disableTask;
            }
        }

        private static async Task Disable(Exception cancellationError, ProtocolSendResultInternal res,
            
            TaskCompletionSource<ProtocolSendResultInternal> cancellationTcs,
            CancellationTokenSource timeoutId, ISendProtocolInternal protocol, IQuasiHttpRequest request)
        {
            timeoutId?.Cancel();

            if (cancellationError != null)
            {
                cancellationTcs?.TrySetException(cancellationError);
            }
            else
            {
                cancellationTcs?.TrySetResult(null);
            }

            if (cancellationError != null || res?.Response?.Body == null || res?.ResponseBufferingApplied == true)
            {
                try
                {
                    await protocol.Cancel();
                }
                catch (Exception) { } // ignore
            }

            // close request body
            if (request != null)
            {
                try
                {
                    await request.CustomDispose();
                }
                catch (Exception) { } // ignore
            }
        }
    }
}
