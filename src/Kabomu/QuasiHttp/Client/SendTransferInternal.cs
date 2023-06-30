using Kabomu.QuasiHttp.Exceptions;
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
        private object _mutex;
        private ISendProtocolInternal _protocol;

        public object Mutex
        {
            set
            {
                _mutex = value;
            }
        }

        public int TimeoutMillis { get; set; }
        public CancellationTokenSource TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public int MaxChunkSize { get; set; }
        public IConnectivityParams ConnectivityParams { get; set; }
        public object Connection { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

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
                            QuasiHttpRequestProcessingException.ReasonCodeTimeout, "send timeout");
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
            Func<SendTransferInternal, ISendProtocolInternal> protocolFactory)
        {
            // even if abort has already happened, still go ahead and
            // create protocol instance and cancel it because the
            // factory may be holding on to some live resources.
            var protocol = protocolFactory.Invoke(this);
            _protocol = protocol;
            if (IsAborted)
            {
                // Oops...connection establishment took so long, or a cancellation happened
                // during connection establishment.
                try
                {
                    await protocol.Cancel();
                }
                catch (Exception) { } // ignore.

                return null;
            }
            var res = await protocol.Send();
            await Abort(null, res);
            return res?.Response;
        }

        public void Abort(Exception error)
        {
            // don't wait
            _ = Abort(error, null);
        }

        private async Task Abort(Exception cancellationError, ProtocolSendResult res)
        {
            Task disableTask = null;
            lock (_mutex)
            {
                if (IsAborted)
                {
                    // dispose off response
                    if (res?.Response != null)
                    {
                        try
                        {
                            // don't wait.
                            _ = res.Response.CustomDispose();
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

        private static async Task Disable(Exception cancellationError, ProtocolSendResult res,
            TaskCompletionSource<IQuasiHttpResponse> cancellationTcs,
            CancellationTokenSource timeoutId, ISendProtocolInternal protocol, IQuasiHttpRequest request)
        {
            if (cancellationTcs != null)
            {
                if (cancellationError != null)
                {
                    cancellationTcs.SetException(cancellationError);
                }
                else
                {
                    cancellationTcs.SetResult(res?.Response);
                }
            }

            timeoutId?.Cancel();

            // just in case cancellation was requested even before transfer protocol could
            // be set up...check to avoid possible null pointer error.
            if (protocol != null)
            {
                if (cancellationError != null || res?.Response?.Body == null || res?.ResponseBufferingApplied == true)
                {
                    await protocol.Cancel();
                }
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
