using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        private readonly object _mutex = new object();

        public IReceiveProtocolInternal Protocol { get; set; }
        public CancellationTokenSource TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public IQuasiHttpRequest Request { get; set; }

        public async Task<IQuasiHttpResponse> StartProtocol(
            IReceiveProtocolInternal protocol)
        {
            Protocol = protocol;
            var res = await protocol.Receive();
            await Abort(res);
            return res;
        }

        public async Task Abort(IQuasiHttpResponse res)
        {
            Task disableTask = null;
            var disposeRes = false;
            lock (_mutex)
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