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
        private ISendProtocolInternal _protocol;

        public IMutexApi MutexApi { get; set; }
        public ITimerApi TimerApi { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; private set; }
        public bool IsAborted { get; private set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public int MaxChunkSize { get; set; }
        public IConnectivityParams ConnectivityParams { get; set; }
        public object Connection { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }
        public bool RequestWrappingEnabled { get; set; }
        public bool ResponseWrappingEnabled { get; set; }

        public void SetTimeout()
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
                await Abort(timeoutError);
            }, TimeoutMillis).Item2;
        }

        public async Task<IQuasiHttpResponse> StartProtocol(
            Func<SendTransferInternal, ISendProtocolInternal> protocolFactory)
        {
            _protocol = protocolFactory.Invoke(this);
            if (IsAborted)
            {
                // Oops...connection establishment took so long, or a cancellation happened
                // during connection establishment.
                // just release the connection.
                try
                {
                    await _protocol.Cancel();
                }
                catch { } // ignore.
                
                return null;
            }
            var res = await _protocol.Send();
            await Abort(null, res);
            return res.Response;
        }

        public Task Abort(Exception error)
        {
            return Abort(error, null);
        }

        private async Task Abort(Exception cancellationError, ProtocolSendResult res)
        {
            Task disableTask;
            using (await MutexApi.Synchronize())
            {
                if (IsAborted)
                {
                    // dispose off response
                    try
                    {
                        await res?.Response?.Close();
                    }
                    catch { } // ignore.

                    // in any case do not proceed with disabling.
                    return;
                }
                disableTask = Disable(cancellationError, res);
            }
            await disableTask;
        }

        private async Task Disable(Exception cancellationError, ProtocolSendResult res)
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

            TimerApi?.ClearTimeout(TimeoutId);

            bool cancelProtocol = false;
            if (cancellationError != null || res?.Response?.Body == null || res?.ResponseBufferingApplied == true)
            {
                cancelProtocol = true;
            }
            if (cancelProtocol)
            {
                // just in case cancellation was requested even before transfer protocol could
                // be set up...check to avoid possible null pointer error.
                await _protocol?.Cancel();
            }

            // close request body
            try
            {
                await Request.Body?.EndRead();
            }
            catch { } // ignore
        }
    }
}
