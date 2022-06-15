using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal interface IParentTransferProtocolInternal
    {
        int DefaultTimeoutMillis { get; }
        IQuasiHttpApplication Application { get; }
        IQuasiHttpTransport Transport { get; }
        IEventLoopApi EventLoop { get; }
        UncaughtErrorCallback ErrorHandler { get; }
        Task<IQuasiHttpResponse> AbortTransferAsync(ITransferProtocolInternal transfer, Exception e);
    }
}
