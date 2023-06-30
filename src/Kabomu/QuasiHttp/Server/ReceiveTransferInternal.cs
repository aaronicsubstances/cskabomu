using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        private object _mutex;
        private IReceiveProtocolInternal _protocol;

        public object Mutex
        {
            set
            {
                _mutex = value;
            }
        }

        public int TimeoutMillis { get; set; }
        public CancellationTokenSource TimeoutId { get; private set; }
        public bool IsAborted { get; private set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
        public object Connection { get; set; }
        public IQuasiHttpTransport Transport { get; set; }

        public void SetTimeout()
        {
            if (TimeoutMillis <= 0)
            {
                return;
            }
            TimeoutId = new CancellationTokenSource();
            Task.Delay(TimeoutMillis, TimeoutId.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        var timeoutError = new QuasiHttpRequestProcessingException(
                            QuasiHttpRequestProcessingException.ReasonCodeTimeout, "receive timeout");
                        Abort(timeoutError);
                    }
                });
        }

        /// <summary>
        /// Assume this call occurs under mutex.
        /// </summary>
        /// <param name="protocolFactory"></param>
        /// <returns></returns>
        public async Task<IQuasiHttpResponse> StartProtocol(
            Func<ReceiveTransferInternal, IReceiveProtocolInternal> protocolFactory)
        {
            // even if abort has already happened, still go ahead and
            // create protocol instance and cancel it because the
            // factory may be holding on to some live resources.
            // NB: the code structure here is meant to mirror that of
            // SendTransferInternal. Over here, there is currently no
            // reason why an abort will occur before protocol instance is creatd.
            var protocol = protocolFactory.Invoke(this);
            _protocol = protocol;
            if (IsAborted)
            {
                try
                {
                    await protocol.Cancel();
                }
                catch (Exception) { } // ignore.

                return null;
            }
            var res = await protocol.Receive();
            await Abort(null, res);
            return res;
        }

        public void Abort(Exception error)
        {
            // don't wait
            _ = Abort(error, null);
        }

        private async Task Abort(Exception cancellationError, IQuasiHttpResponse res)
        {
            Task disableTask = null;
            lock (_mutex)
            {
                if (IsAborted)
                {
                    // dispose off response
                    if (res != null)
                    {
                        try
                        {
                            // don't wait.
                            _ = res.CustomDispose();
                        }
                        catch (Exception) { } // ignore.
                    }

                    // in any case do not proceed with disabling.
                    return;
                }
                IsAborted = true;
                disableTask = Disable(cancellationError, res,
                    CancellationTcs, TimeoutId, _protocol, Request);
            }
            if (disableTask != null)
            {
                await disableTask;
            }
        }

        private static async Task Disable(Exception cancellationError, IQuasiHttpResponse res,
            TaskCompletionSource<IQuasiHttpResponse> cancellationTcs,
            CancellationTokenSource timeoutId, IReceiveProtocolInternal protocol, IQuasiHttpRequest request)
        {
            if (cancellationTcs != null)
            {
                if (cancellationError != null)
                {
                    cancellationTcs.SetException(cancellationError);
                }
                else
                {
                    cancellationTcs.SetResult(res);
                }
            }

            timeoutId?.Cancel();

            if (protocol != null)
            {
                await protocol.Cancel();
            }

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