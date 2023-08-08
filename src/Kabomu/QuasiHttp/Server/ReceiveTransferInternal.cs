using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        private int _abortCalled;

        public IReceiveProtocolInternal Protocol { get; set; }
        public CancellationTokenSource TimeoutId { get; set; }
        public bool IsAborted => _abortCalled != 0;
        public IQuasiHttpRequest Request { get; set; }

        public bool TrySetAborted()
        {
            return Interlocked.CompareExchange(ref _abortCalled, 1, 0) == 0;
        }

        public async Task<IQuasiHttpResponse> StartProtocol()
        {
            var res = await Protocol.Receive();
            await Abort(res);
            return res;
        }

        public async Task Abort(IQuasiHttpResponse res)
        {
            if (TrySetAborted())
            {
                TimeoutId?.Cancel();

                try
                {
                    await Protocol.Cancel();
                }
                catch (Exception) { } // ignore

                // close body of request received for direct send to application
                if (Request != null)
                {
                    try
                    {
                        await Request.Release();
                    }
                    catch (Exception) { }
                }
            }
            else
            {
                // dispose off response
                try
                {
                    var resDisposeTask = res?.Release();
                    if (resDisposeTask != null)
                    {
                        await resDisposeTask;
                    }
                }
                catch (Exception) { } // ignore.
            }
        }
    }
}