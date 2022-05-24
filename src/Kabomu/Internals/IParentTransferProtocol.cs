using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal interface IParentTransferProtocol
    {
        int DefaultTimeoutMillis { get; }
        int MaxRetryPeriodMillis { get; }
        int MaxRetryCount { get; }
        IQuasiHttpApplication Application { get; }
        IQuasiHttpTransport Transport { get; }
        public IEventLoopApi EventLoop { get; }
        public UncaughtErrorCallback ErrorHandler { get; }
        void AbortTransfer(ITransferProtocol transfer, Exception e);
    }
}
