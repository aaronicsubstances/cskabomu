﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        private int _abortCalled;

        public IReceiveProtocolInternal Protocol { get; set; }
        public CancellablePromiseInternal<IQuasiHttpResponse> TimeoutId { get; set; }
        public bool IsAborted => _abortCalled != 0;

        public bool TrySetAborted()
        {
            return Interlocked.CompareExchange(ref _abortCalled, 1, 0) == 0;
        }

        public async Task<IQuasiHttpResponse> StartProtocol()
        {
            var res = await Protocol.Receive();
            await Abort();
            return res;
        }

        public async Task Abort()
        {
            if (TrySetAborted())
            {
                TimeoutId.Cancel(); // TimeoutId?.Cancel()

                try
                {
                    await Protocol.Cancel();
                }
                catch (Exception) { } // ignore
            }
        }
    }
}