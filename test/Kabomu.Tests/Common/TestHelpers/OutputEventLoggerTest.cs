using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class OutputEventLoggerTest
    {
        private readonly ITestOutputHelper outputHelper;

        public OutputEventLoggerTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void TestLogMethods()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var instance = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };

            // act
            instance.AppendTransferError(new Exception("required"), null);
            instance.AppendTransferError(null, "optional");
            instance.AppendEventLoopError(new Exception("x"), null);
            instance.AppendEventLoopError(null, "invalid");
            instance.AppendOnReceivePduLog(null, 1, 2, 3, 4, 5, null, 0, 0, 9, null);
            instance.AppendOnReceivePduLog("c", 1, 2, 3, 4, 5, new byte[0], 0, 0, null, new DefaultCancellationIndicator());
            instance.AppendOnReceivePduLog("d", 10, 21, 23, 24, 85, new byte[] { (byte)'a', (byte)'f' }, 
                0, 2, null, OutputEventLogger.CreateAlreadyCancelledIndicator());

            testEventLoop.AdvanceTimeTo(23);

            instance.AppendSinkCreationLog();
            instance.AppendSinkWriteDataLog(new byte[] { 0x7, (byte)'z', (byte)'y' }, 1, 1, null, true);
            instance.AppendSinkWriteDataLog(new byte[] { (byte)'b', (byte)'o', (byte)'y' }, 0, 3, "f", false);
            instance.AppendSinkWriteDataLog(new byte[] { (byte)'b', (byte)'o', (byte)'y' }, 0, 0, "yes", true);
            instance.AppendSinkOnEndWriteLog(null);
            instance.AppendSinkOnEndWriteLog(new Exception("cancelled"));

            testEventLoop.AdvanceTimeTo(40);

            instance.AppendSourceReadDataLog();
            instance.AppendSourceOnEndReadLog(null);
            instance.AppendSourceOnEndReadLog(new Exception("8:duplicate"));

            testEventLoop.AdvanceTimeTo(50);

            instance.AppendCallbackLog(null);
            instance.AppendCallbackLog(new Exception("14:"));

            testEventLoop.AdvanceTimeTo(60);

            // assert
            var expectedLogs = new List<string>
            {
                "0:TransferError(required,)",
                "0:TransferError(,optional)",
                "0:EvError(x,)",
                "0:EvError(,invalid)",

                "0:TransferChunk(,1,2,3,4,5,,9,)",
                "0:TransferChunk(c,1,2,3,4,5,,,false)",
                "0:TransferChunk(d,10,21,23,24,85,af,,true)",

                "23:SnkCreate()",
                "23:SnkData(z,,true)",
                "23:SnkData(boy,f,false)",
                "23:SnkData(,yes,true)",
                "23:SnkEnd()",
                $"23:SnkEnd(cancelled)",

                "40:SrcData()",
                "40:SrcEnd()",
                $"40:SrcEnd(8)",

                "50:Cb()",
                $"50:Cb(14)"
            };
            instance.AssertEqual(expectedLogs, outputHelper);
        }
    }
}
