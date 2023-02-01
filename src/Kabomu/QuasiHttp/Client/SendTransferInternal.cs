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
    internal class SendTransferInternal
    {
        public IMutexApi MutexApi { get; set; }
        public ITimerApi TimerApi { get; set; }
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public ISendProtocolInternal Protocol { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public object BypassCancellationHandle { get; set; }
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

        public async Task Abort(Exception cancellationError, ProtocolSendResult res)
        {
            try
            {
                Task disableTransferTask;
                using (await MutexApi.Synchronize())
                {
                    if (IsAborted)
                    {
                        return;
                    }
                    disableTransferTask = Disable(cancellationError, res);
                }
                await disableTransferTask;
            }
            catch { } // ignore
        }

        private async Task Disable(Exception cancellationError, ProtocolSendResult res)
        {
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
            IsAborted = true;
            if (TimeoutId != null)
            {
                TimerApi.ClearTimeout(TimeoutId);
            }
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
            if (Request.Body != null)
            {
                try
                {
                    // no need to synchronize
                    await Request.Body.EndRead();
                }
                catch { } // ignore
            }
        }
    }
}
