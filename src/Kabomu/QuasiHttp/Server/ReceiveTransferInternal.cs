using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal : IRequestProcessorInternal
    {
        public ITimerApi TimerApi { get; set; }
        public IMutexApi MutexApi { get; set; }
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public int TimeoutMillis { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public IQuasiHttpProcessingOptions ProcessingOptions { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
        public int MaxChunkSize { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public IReceiveProtocolInternal Protocol { get; set; }

        public void SetReceiveTimeout()
        {
            if (TimeoutMillis <= 0)
            {
                return;
            }
            var timer = TimerApi;
            if (timer == null)
            {
                throw new MissingDependencyException("timer api");
            }
            TimeoutId = timer.WhenSetTimeout(async () =>
            {
                var timeoutError = new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeTimeout, "receive timeout");
                await Abort(timeoutError, null);
            }, TimeoutMillis).Item2;
        }

        public async Task<IQuasiHttpResponse> StartProtocol()
        {
            var res = await Protocol.Receive();
            await Abort(null, res);
            return res;
        }

        public Task AbortWithError(Exception error)
        {
            return Abort(error, null);
        }

        public async Task Abort(Exception cancellationError, IQuasiHttpResponse res)
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
            MutexApi = null;
            await disableTransferTask;
        }

        private async Task Disable(Exception cancellationError,
            IQuasiHttpResponse res)
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
                    CancellationTcs.SetResult(res);
                }
            }
            CancellationTcs = null;
            if (TimeoutId != null)
            {
                TimerApi.ClearTimeout(TimeoutId);
            }
            TimeoutId = null;
            TimerApi = null;
            if (Connection != null)
            {
                try
                {
                    await Transport.ReleaseConnection(Connection);
                }
                catch (Exception) { }
            }
            Connection = null;
            if (Protocol != null)
            {
                try
                {
                    Protocol.Cancel();
                }
                catch { } // ignore
            }
            Protocol = null;
            // close body of send to application request
            if (Request?.Body != null)
            {
                try
                {
                    await Request.Body.EndRead();
                }
                catch (Exception) { }
            }
            Request = null;
            RequestEnvironment = null;
            Transport = null;
            ProcessingOptions = null;
        }
    }
}