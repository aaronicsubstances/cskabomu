using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class SendTransferInternal : IRequestProcessorInternal
    {
        public IMutexApi MutexApi { get; set; }
        public ITimerApi TimerApi { get; set; }
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public ISendProtocolInternal Protocol { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public DefaultConnectivityParams ConnectivityParams { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public IQuasiHttpSendOptions SendOptions { get; set; }
        public int TimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }
        public bool RequestWrappingEnabled { get; set; }
        public bool ResponseWrappingEnabled { get; set; }

        public void SetSendTimeout()
        {
            if (TimeoutMillis <= 0)
            {
                return;
            }
            if (TimerApi == null)
            {
                throw new MissingDependencyException("timer api");
            }
            TimeoutId = TimerApi.WhenSetTimeout(async () =>
            {
                var timeoutError = new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeTimeout, "send timeout");
                await Abort(timeoutError, null);
            }, TimeoutMillis).Item2;
        }

        public async Task<IQuasiHttpResponse> StartProtocol()
        {
            var res = await Protocol.Send(Request);
            await Abort(null, res);
            return res.Response;
        }

        public Task AbortWithError(Exception error)
        {
            return Abort(error, null);
        }

        public async Task Abort(Exception cancellationError, ProtocolSendResult res)
        {
            using (await MutexApi.Synchronize())
            {
                if (IsAborted)
                {
                    // dispose off response
                    try
                    {
                        // don't wait.
                        res?.Response?.Close();
                    }
                    catch { } // ignore.
                    return;
                }
                Disable(cancellationError, res);
            }
        }

        private void Disable(Exception cancellationError, ProtocolSendResult res)
        {
            IsAborted = true;

            if (CancellationTcs != null)
            {
                if (cancellationError != null)
                {
                    CancellationTcs.SetException(cancellationError);
                }
                else
                {
                    CancellationTcs.SetResult(res?.Response);
                }
            }
            CancellationTcs = null;
            if (TimeoutId != null)
            {
                TimerApi.ClearTimeout(TimeoutId);
            }
            TimeoutId = null;
            TimerApi = null;
            bool cancelProtocol = false;
            if (cancellationError != null || res?.Response?.Body == null || res?.ResponseBufferingApplied == true)
            {
                cancelProtocol = true;
            }
            if (cancelProtocol)
            {
                try
                {
                    // just in case cancellation was requested even before transfer protocol could
                    // be set up...check to avoid possible null pointer error.
                    Protocol?.Cancel();
                }
                catch { } // ignore
            }
            Protocol = null;
            if (Request.Body != null)
            {
                try
                {
                    // don't wait.
                    _ = Request.Body.EndRead();
                }
                catch { } // ignore
            }
            Request = null;
            ConnectivityParams = null;
            SendOptions = null;
        }
    }
}
