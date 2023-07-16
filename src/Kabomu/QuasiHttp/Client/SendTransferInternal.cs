﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class SendTransferInternal
    {
        private readonly object _mutex = new object();

        public ISendProtocolInternal Protocol { get; set; }
        public CancellationTokenSource TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public TaskCompletionSource<ProtocolSendResultInternal> CancellationTcs { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        public async Task<ProtocolSendResultInternal> StartProtocol(ISendProtocolInternal protocol)
        {
            Protocol = protocol;
            var res = await protocol.Send();
            await Abort(null, res);
            return res;
        }

        public  async Task Abort(Exception cancellationError, ProtocolSendResultInternal res)
        {
            Task disableTask = null;
            var disposeRes = false;
            lock (_mutex)
            {
                if (IsAborted)
                {
                    disposeRes = true;
                }
                else
                {
                    IsAborted = true;
                    disableTask = Disable(cancellationError, res,
                        CancellationTcs, TimeoutId, Protocol, Request);
                }
            }
            if (disposeRes)
            {
                // dispose off response
                try
                {
                    var resDisposeTask = res?.Response?.CustomDispose();
                    if (resDisposeTask != null)
                    {
                        await resDisposeTask;
                    }
                }
                catch (Exception) { } // ignore.
            }
            if (disableTask != null)
            {
                await disableTask;
            }
        }

        private static async Task Disable(Exception cancellationError, ProtocolSendResultInternal res,
            
            TaskCompletionSource<ProtocolSendResultInternal> cancellationTcs,
            CancellationTokenSource timeoutId, ISendProtocolInternal protocol, IQuasiHttpRequest request)
        {
            timeoutId?.Cancel();

            if (cancellationError != null)
            {
                cancellationTcs?.TrySetException(cancellationError);
            }
            else
            {
                cancellationTcs?.TrySetResult(null);
            }

            if (cancellationError != null || res?.Response?.Body == null || res?.ResponseBufferingApplied == true)
            {
                try
                {
                    await protocol.Cancel();
                }
                catch (Exception) { } // ignore
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
