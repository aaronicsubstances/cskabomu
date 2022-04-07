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
        public void TestSendSuccessCases()
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

            var msgSource3 = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    return new ConfigurableMessageSourceResult
                    {
                        Delays = new int[] { 2, 3 },
                        FallbackPayload = "morning",
                        HasMore = false,
                    };
                }
            };
            DefaultMessageTransferOptions options3 = null;
            
            var msgSource4Field = 0;
            var msgSource4 = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    msgSource4Field++;
                    if (msgSource4Field == 1)
                    {
                        return new ConfigurableMessageSourceResult
                        {
                            Delays = new int[] { 2, 3 },
                            Data = new byte[] { (byte)'1', (byte)'0', (byte)'1' },
                            Length = 3,
                            HasMore = true,
                        };
                    }
                    else if (msgSource4Field == 2)
                    {
                        return new ConfigurableMessageSourceResult
                        {
                            Delays = new int[] { 1 },
                            Data = new byte[] { 0, (byte)'2', (byte)'0', (byte)'0', (byte)'2', 0 },
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
                            FallbackPayload = "3",
                            HasMore = false,
                        };
                    }
                }
            };
            var options4 = new DefaultMessageTransferOptions
            {
                TimeoutMillis = 60
            };

            // act
            testEventLoop.ScheduleTimeout(2, _ =>
            {
                instance.BeginSend("kumasi", msgSource1, options1, commonCb, "tree");
            }, null);
            testEventLoop.ScheduleTimeout(2, _ =>
            {
                instance.BeginSend(null, msgSource2, options2, commonCb, null);
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
            testEventLoop.ScheduleTimeout(14, _ =>
            {
                instance.BeginSendStartedAtReceiver("6", msgSource3, 7, options3, commonCb, null);
            }, null);
            testEventLoop.ScheduleTimeout(15, _ =>
            {
                instance.BeginSendStartedAtReceiver("1256", msgSource3, 7, options3, commonCb, "y");
            }, null);
            testEventLoop.ScheduleTimeout(16, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 0, 2, 0);
            }, null);
            testEventLoop.ScheduleTimeout(17, _ =>
            {
                instance.BeginSendStartedAtReceiver(null, msgSource4, 8, options4, commonCb, null);
            }, null);
            testEventLoop.ScheduleTimeout(20, _ =>
            {
                instance.OnReceiveFirstChunkAck(null, 128, 7, 0);
            }, null);
            testEventLoop.ScheduleTimeout(21, _ =>
            {
                instance.OnReceiveFirstChunkAck(null, 128, 8, 0);
            }, null);
            testEventLoop.ScheduleTimeout(26, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 128, 8, 0);
            }, null);
            testEventLoop.ScheduleTimeout(30, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 128, 8, 0);
            }, null);
            testEventLoop.ScheduleTimeout(31, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 128, 1, 0);
            }, null);
            testEventLoop.ScheduleTimeout(32, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 128, 2, 0);
            }, null);
            testEventLoop.ScheduleTimeout(33, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 128, 7, 0);
            }, null);
            testEventLoop.ScheduleTimeout(34, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 128, 8, 0);
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
            expectedLogs.Add("14:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg3a
            expectedLogs.Add("15:" +
                OutputEventLogger.CreateSourceOnEndReadLog("" + DefaultMessageTransferManager.ErrorCodeAbortedByReceiver)); // msg3a
            expectedLogs.Add("15:" +
                OutputEventLogger.CreateCallbackLog(null, "" + DefaultMessageTransferManager.ErrorCodeAbortedByReceiver)); // msg3a
            expectedLogs.Add("15:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg3b
            expectedLogs.Add("16:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null)); // msg2
            expectedLogs.Add("16:" +
                OutputEventLogger.CreateCallbackLog(null, null)); // msg2
            expectedLogs.Add("17:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg4
            expectedLogs.Add("17:" +
                OutputEventLogger.CreateOnReceivePduLog("1256", DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeFirstChunk, 128, 0, 7,
                    null, "morning", null)); // msg3b
            expectedLogs.Add("19:" +
                OutputEventLogger.CreateOnReceivePduLog(null, DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeFirstChunk, 192, 0, 8,
                    "101", null, null)); // msg3b
            expectedLogs.Add("20:" +
                 OutputEventLogger.CreateSourceOnEndReadLog(null)); // msg3b
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateCallbackLog("y", null)); // msg3b
            expectedLogs.Add("21:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg4
            expectedLogs.Add("22:" +
                OutputEventLogger.CreateOnReceivePduLog(null, DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeSubsequentChunk, 192, 0, 8,
                    "2002", null, null));
            expectedLogs.Add("26:" +
                OutputEventLogger.CreateSourceReadDataLog()); // msg4
            expectedLogs.Add("26:" +
                OutputEventLogger.CreateOnReceivePduLog(null, DefaultProtocolDataUnit.Version01,
                    DefaultProtocolDataUnit.PduTypeSubsequentChunk, 128, 0, 8,
                    null, "3", null));
            expectedLogs.Add("30:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null)); // msg4
            expectedLogs.Add("30:" +
                OutputEventLogger.CreateCallbackLog(null, null)); // msg4

            logger.AssertEqual(expectedLogs, outputHelper);
        }
    }
}
