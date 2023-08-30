﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class SendTransferInternal
    {
        private int _abortCalled;

        public ISendProtocolInternal Protocol { get; set; }
        public CancellablePromiseInternal<ProtocolSendResultInternal> TimeoutId { get; set; }
        public bool IsAborted => _abortCalled != 0;
        public TaskCompletionSource<ProtocolSendResultInternal> CancellationTcs { get; set; }

        public bool TrySetAborted()
        {
            return Interlocked.CompareExchange(ref _abortCalled, 1, 0) == 0;
        }

        public async Task<ProtocolSendResultInternal> StartProtocol()
        {
            var res = await Protocol.Send();
            await Abort(null, res);
            return res;
        }

        public async Task Abort(Exception cancellationError, ProtocolSendResultInternal res)
        {
            if (TrySetAborted())
            {
                TimeoutId.Cancel(); // TimeoutId?.Cancel()

                if (cancellationError != null)
                {
                    CancellationTcs?.TrySetException(cancellationError);
                }
                else
                {
                    CancellationTcs?.TrySetResult(null);
                }

                if (cancellationError != null || res?.Response?.Body == null
                    || res?.ResponseBufferingApplied == true)
                {
                    try
                    {
                        if (cancellationError != null)
                        {
                            await Protocol.Cancel();
                        }
                        else
                        {
                            _ = Protocol.Cancel();
                        }
                    }
                    catch (Exception) { } // ignore
                }
            }
            else
            {
                // dispose off response
                try
                {
                    var resDisposeTask = res?.Response?.Release();
                    if (resDisposeTask != null && cancellationError != null)
                    {
                        await resDisposeTask;
                    }
                }
                catch (Exception) { } // ignore.
            }
        }
    }
}
