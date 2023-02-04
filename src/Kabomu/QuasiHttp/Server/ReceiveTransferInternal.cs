using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        private readonly object _mutex = new object();
        private IReceiveProtocolInternal _protocol;

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
            // even if abort has already happened, still go ahead and
            // create protocol instance and cancel it because the
            // factory may be holding on to some live resources.
            var protocol = protocolFactory.Invoke(this);
            bool abortedAlready = false;
            lock (_mutex)
            {
                _protocol = protocol;
                if (IsAborted)
                {
                    abortedAlready = true;
                }
            }
            if (abortedAlready)
            {
                try
                {
                    await protocol.Cancel();
                }
                catch { } // ignore.

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
                            _ = res.Close();
                        }
                        catch { } // ignore.
                    }

                    // in any case do not proceed with disabling.
                    return;
                }
                IsAborted = true;
                disableTask = Disable(cancellationError, res,
                    CancellationTcs, TimerApi, TimeoutId, _protocol, Request?.Body);
            }
            if (disableTask != null)
            {
                await disableTask;
            }
        }

        private static async Task Disable(Exception cancellationError, IQuasiHttpResponse res,
            TaskCompletionSource<IQuasiHttpResponse> cancellationTcs, ITimerApi timerApi,
            object timeoutId, IReceiveProtocolInternal protocol, IQuasiHttpBody requestBody)
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

            timerApi?.ClearTimeout(timeoutId);

            if (protocol != null)
            {
                await protocol.Cancel();
            }

            // close body of request received for direct send to application
            if (requestBody != null)
            {
                try
                {
                    await requestBody.EndRead();
                }
                catch (Exception) { }
            }
        }
    }
}