using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class RequestCancellationHandle : ICustomDisposable
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public async Task Release()
        {
            CancellationTokenSource?.Cancel();
            if (Request != null)
            {
                try
                {
                    await Request.Release();
                }
                catch { } // ignore
            }
        }
    }
}
