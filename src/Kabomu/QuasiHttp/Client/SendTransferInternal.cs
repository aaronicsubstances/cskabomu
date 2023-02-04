﻿using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
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
        private readonly object _mutex = new object();
        private ISendProtocolInternal _protocol;

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
            TimeoutId = TimerApi.SetTimeout(() =>
            {
                var timeoutError = new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeTimeout, "send timeout");
                Abort(timeoutError);
            }, TimeoutMillis);
        }

        public async Task<IQuasiHttpResponse> StartProtocol(
            Func<SendTransferInternal, ISendProtocolInternal> protocolFactory)
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
                    // Oops...connection establishment took so long, or a cancellation happened
                    // during connection establishment.
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
                            _ = res.Response.Close();
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

        private static async Task Disable(Exception cancellationError, ProtocolSendResult res,
            TaskCompletionSource<IQuasiHttpResponse> cancellationTcs, ITimerApi timerApi,
            object timeoutId, ISendProtocolInternal protocol, IQuasiHttpBody requestBody)
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

            timerApi?.ClearTimeout(timeoutId);

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
            if (requestBody != null)
            {
                try
                {
                    await requestBody.EndRead();
                }
                catch { } // ignore
            }
        }
    }
}
