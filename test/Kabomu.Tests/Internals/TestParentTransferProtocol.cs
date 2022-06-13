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
    }
}
