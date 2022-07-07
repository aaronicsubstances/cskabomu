﻿using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Internals
{
    internal class TestParentTransferProtocol : IParentTransferProtocolInternal
    {
        private readonly ITransferProtocolInternal _expectedTransfer;

        public TestParentTransferProtocol(ITransferProtocolInternal expectedTransfer)
        {
            _expectedTransfer = expectedTransfer;
        }

        public bool AbortCalled { get; private set; }

        public Task AbortTransfer(ITransferProtocolInternal transfer)
        {
            Assert.False(AbortCalled);
            Assert.Equal(_expectedTransfer, transfer);
            AbortCalled = true;
            return Task.CompletedTask;
        }
    }
}
