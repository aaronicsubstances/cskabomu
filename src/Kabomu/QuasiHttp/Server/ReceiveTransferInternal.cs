using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        public object Mutex { get; set; }
        public IReceiveProtocolInternal Protocol { get; set; }
        public CancellationTokenSource TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
        public object Connection { get; set; }
        public IQuasiHttpTransport Transport { get; set; }

        /// <summary>
        /// Assume this call occurs under mutex.
        /// </summary>
        /// <param name="protocolFactory"></param>
        /// <returns></returns>
        public async Task<IQuasiHttpResponse> StartProtocol(
            Func<ReceiveTransferInternal, IReceiveProtocolInternal> protocolFactory)
        {
            var protocol = protocolFactory.Invoke(this);
            Protocol = protocol;
            var res = await protocol.Receive();
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
            await Abort(res);
            return res;
        }

        public async Task Abort(IQuasiHttpResponse res)
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
                    disableTask = Disable(TimeoutId, Protocol, Request);
                }
            }
            if (disposeRes)
            {
                // dispose off response
                try
                {
                    var resDisposeTask = res?.CustomDispose();
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

        private static async Task Disable(CancellationTokenSource timeoutId,
            IReceiveProtocolInternal protocol, IQuasiHttpRequest request)
        {
            timeoutId?.Cancel();

            try
            {
                await protocol.Cancel();
            }
            catch (Exception) { } // ignore

            // close body of request received for direct send to application
            if (request != null)
            {
                try
                {
                    await request.CustomDispose();
                }
                catch (Exception) { }
            }
        }
    }
}