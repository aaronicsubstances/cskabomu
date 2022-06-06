using Kabomu.Common;
using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    internal class TestParentTransferProtocol : IParentTransferProtocol
    {
        private readonly ITransferProtocol _expectedTransfer;

        public TestParentTransferProtocol(ITransferProtocol expectedTransfer)
        {
            _expectedTransfer = expectedTransfer;
        }

        public int DefaultTimeoutMillis { get; set; }

        public IQuasiHttpApplication Application { get; set; }

        public IQuasiHttpTransport Transport { get; set; }

        public IMutexApi Mutex { get; set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }
        public bool AbortCalled { get; private set; }

        public void AbortTransfer(ITransferProtocol transfer, Exception e)
        {
            Assert.False(AbortCalled);
            Assert.Equal(_expectedTransfer, transfer);
            Assert.Null(e);
            AbortCalled = true;
        }

        public void ReadBytesFullyFromTransport(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            TransportUtils.ReadBytesFully(Transport, connection, data, offset, length, e =>
            {
                cb.Invoke(e);
                // test handling of repeated callback invocations
                cb.Invoke(e);
            });
        }

        public void TransferBodyToTransport(object connection, IQuasiHttpBody body, Action<Exception> cb)
        {
            TransportUtils.TransferBodyToTransport(Transport, connection, body, Mutex, e =>
            {
                cb.Invoke(e);
                // test handling of repeated callback invocations
                cb.Invoke(e);
            });
        }

        public void WriteByteSlices(object connection, ByteBufferSlice[] slices, Action<Exception> cb)
        {
            ProtocolUtils.WriteByteSlices(Transport, connection, slices, e =>
            {
                cb.Invoke(e);
                // test handling of repeated callback invocations
                cb.Invoke(e);
            });
        }
    }
}
