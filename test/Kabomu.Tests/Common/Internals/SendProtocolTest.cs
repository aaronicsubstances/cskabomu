using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using Kabomu.Common.Internals;
using Kabomu.Tests.Common.TestHelpers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.Internals
{
    public class SendProtocolTest
    {
        private readonly ITestOutputHelper outputHelper;

        public SendProtocolTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void TestBeginSend()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };

            var qpcService = new ConfigurableQpcFacility
            {
                EventLoop = testEventLoop,
                Logger = logger,
                SendPduCallbackInstance = (object connectionHandle,
                    byte version, byte pduType, byte flags,
                    byte errorCode, long messageId,
                    byte[] data, int offset, int length, object fallbackPayload,
                    ICancellationIndicator cancellationIndicator) =>
                {
                    return new ConfigurableSendPduResult
                    {
                        Delays = new int[] { 1, 3, 6 }
                    };
                }
            };

            var instance = new SendProtocol
            {
                DefaultTimeoutMillis = 10,
                EventLoop = testEventLoop,
                QpcService = qpcService,
                MessageIdGenerator = new PredictableMessageIdGenerator()
            };
            Action<object, Exception> commonCb = (s, e) =>
            {
                logger.AppendCallbackLog(s, e);
            };

            var msgSource1 = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    return new ConfigurableMessageSourceResult
                    {
                        Delays = new int[] { 2, 3 },
                        FallbackPayload = "tea",
                        HasMore = false,
                    };
                }
            };
            DefaultMessageTransferOptions options1 = null;

            var msgSource2Field = 0;
            var msgSource2 = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    msgSource2Field++;
                    if (msgSource2Field == 1)
                    {
                        return new ConfigurableMessageSourceResult
                        {
                            Delays = new int[] { 2, 3 },
                            Data = new byte[] { (byte)'i', (byte)'c', (byte)'e' },
                            Length = 3,
                            HasMore = true,
                        };
                    }
                    else if (msgSource2Field == 2)
                    {
                        return new ConfigurableMessageSourceResult
                        {
                            Delays = new int[] { 1 },
                            Data = new byte[] { 0, (byte)' ', (byte)'c', (byte)'u', (byte)'p', 0 },
                            Offset = 1,
                            Length = 4,
                            HasMore = true,
                        };
                    }
                    else
                    {
                        return new ConfigurableMessageSourceResult
                        {
                            Delays = new int[] { 0, 2, 3 },
                            FallbackPayload = "s",
                            HasMore = false,
                        };
                    }
                }
            };
            var options2 = new DefaultMessageTransferOptions
            {
                TimeoutMillis = 60
            };

            // act
            testEventLoop.ScheduleTimeout(2, _ =>
            {
                long msgId = instance.BeginSend("kumasi", msgSource1, options1, commonCb, "tree");
                Assert.Equal(1, msgId);
            }, null);
            testEventLoop.ScheduleTimeout(2, _ =>
            {
                long msgId = instance.BeginSend(null, msgSource2, options2, commonCb, null);
                Assert.Equal(2, msgId);
            }, null);
            testEventLoop.ScheduleTimeout(7, _ =>
            {
                instance.OnReceiveFirstChunkAck(null, 0, 2, 0);
            }, null);
            testEventLoop.ScheduleTimeout(8, _ =>
            {
                instance.OnReceiveFirstChunkAck("kumawu", 0, 1, 0);
            }, null);
            testEventLoop.ScheduleTimeout(12, _ =>
            {
                instance.OnReceiveSubsequentChunkAck("tema", 0, 2, 0);
            }, null);
            testEventLoop.ScheduleTimeout(16, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 0, 2, 0);
            }, null);
            testEventLoop.AdvanceTimeBy(100);

            // assert
            Assert.Equal(0, testEventLoop.PendingEventCount);

            var expectedLogs = new List<string>();
            expectedLogs.Add("2:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg1
            expectedLogs.Add("2:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg2
            expectedLogs.Add("4:" +
                OutputEventLogger.CreateOnReceivePduLog("kumasi", DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeFirstChunk, 0, 0, 1,
                    null, "tea", null));
            expectedLogs.Add("4:" +
                OutputEventLogger.CreateOnReceivePduLog(null, DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeFirstChunk, 64, 0, 2,
                    "ice", null, null));
            expectedLogs.Add("7:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg2
            expectedLogs.Add("8:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null)); // msg1
            expectedLogs.Add("8:" +
                OutputEventLogger.CreateCallbackLog("tree", null)); // msg1
            expectedLogs.Add("8:" +
                OutputEventLogger.CreateOnReceivePduLog(null, DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeSubsequentChunk, 64, 0, 2,
                    " cup", null, null));
            expectedLogs.Add("12:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg2
            expectedLogs.Add("12:" +
                OutputEventLogger.CreateOnReceivePduLog("tema", DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeSubsequentChunk, 0, 0, 2,
                    null, "s", null));
            expectedLogs.Add("16:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null)); // msg2
            expectedLogs.Add("16:" +
                OutputEventLogger.CreateCallbackLog(null, null)); // msg2

            logger.AssertEqual(expectedLogs, outputHelper);
        }
    }
}
