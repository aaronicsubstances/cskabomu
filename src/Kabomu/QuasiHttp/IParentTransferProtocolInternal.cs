using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    internal interface IParentTransferProtocolInternal
    {
        int DefaultTimeoutMillis { get; }
        IQuasiHttpApplication Application { get; }
        IQuasiHttpTransport Transport { get; }
        public IMutexApi Mutex { get; }
        public UncaughtErrorCallback ErrorHandler { get; }
        void AbortTransfer(ITransferProtocolInternal transfer, Exception e);
    }
}
