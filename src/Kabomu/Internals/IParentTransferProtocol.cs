using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal interface IParentTransferProtocol
    {
        int DefaultTimeoutMillis { get; }
        IQuasiHttpApplication Application { get; }
        IQuasiHttpTransport Transport { get; }
        public IMutexApi Mutex { get; }
        public UncaughtErrorCallback ErrorHandler { get; }
        void AbortTransfer(ITransferProtocol transfer, Exception e);
        void TransferBodyToTransport(object connection, IQuasiHttpBody body, Action<Exception> cb);
        void ReadBytesFullyFromTransport(object connection, byte[] data, int offset, int length, Action<Exception> cb);
    }
}
