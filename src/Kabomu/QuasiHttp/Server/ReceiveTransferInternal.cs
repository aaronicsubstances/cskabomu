using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        private IReceiveProtocolInternal _protocol;

        public IMutexApi MutexApi { get; set; }
        public ITimerApi TimerApi { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; private set; }
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
            var timer = TimerApi;
            if (timer == null)
            {
                throw new MissingDependencyException("timer api");
            }
            TimeoutId = timer.SetTimeout(() =>
            {
                var timeoutError = new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeTimeout, "receive timeout");
                Abort(timeoutError);
            }, TimeoutMillis);
        }

        public async Task<IQuasiHttpResponse> StartProtocol(
            Func<ReceiveTransferInternal, IReceiveProtocolInternal> protocolFactory)
        {
            _protocol = protocolFactory.Invoke(this);
            if (IsAborted)
            {
                try
                {
                    await _protocol.Cancel();
                }
                catch { } // ignore.

                return null;
            }
            var res = await _protocol.Receive();
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
            Task disableTask;
            using (await MutexApi.Synchronize())
            {
                if (IsAborted)
                {
                    // dispose off response
                    if (res != null)
                    {
                        try
                        {
                            await res.Close();
                        }
                        catch { } // ignore.
                    }

                    // in any case do not proceed with disabling.
                    return;
                }
                disableTask = Disable(cancellationError, res);
            }
            await disableTask;
        }

        private async Task Disable(Exception cancellationError, IQuasiHttpResponse res)
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

            TimerApi?.ClearTimeout(TimeoutId);

            if (_protocol != null)
            {
                await _protocol.Cancel();
            }

            // close body of request received for direct send to application
            if (Request?.Body != null)
            {
                try
                {
                    await Request.Body.EndRead();
                }
                catch (Exception) { }
            }
        }
    }
}