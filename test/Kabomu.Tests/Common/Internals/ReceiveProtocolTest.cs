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
    public class ReceiveProtocolTest
    {
        private readonly ITestOutputHelper outputHelper;

        public ReceiveProtocolTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void TestReceiveSuccessCases()
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

            var instance = new ReceiveProtocol
            {
                DefaultTimeoutMillis = 10,
                EventLoop = testEventLoop,
                QpcService = qpcService
            };
            Action<object, Exception> commonCb = (s, e) =>
            {
                logger.AppendCallbackLog(s, e);
            };

            var msgSink1 = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (byte[] data, int offset, int length,
                    object fallbackPayload, bool isMoreExpected) =>
                {
                    return new ConfigurableMessageSinkResult
                    {
                        Delays = new int[] { 2, 3 },
                    };
                }
            };
            DefaultMessageTransferOptions options1 = null;

            var msgSink2Field = 0;
            var msgSink2 = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (byte[] data, int offset, int length,
                    object fallbackPayload, bool isMoreExpected) =>
                {
                    msgSink2Field++;
                    if (msgSink2Field == 1)
                    {
                        return new ConfigurableMessageSinkResult
                        {
                            Delays = new int[] { 2, 3 },
                        };
                    }
                    else if (msgSink2Field == 2)
                    {
                        return new ConfigurableMessageSinkResult
                        {
                            Delays = new int[] { 1 },
                        };
                    }
                    else
                    {
                        return new ConfigurableMessageSinkResult
                        {
                            Delays = new int[] { 0, 2, 3 },
                        };
                    }
                }
            };
            var options2 = new DefaultMessageTransferOptions
            {
                TimeoutMillis = 60
            };

            var msgSink3 = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (byte[] data, int offset, int length,
                    object fallbackPayload, bool isMoreExpected) =>
                {
                    return new ConfigurableMessageSinkResult
                    {
                        Delays = new int[] { 2, 3 },
                    };
                }
            };
            DefaultMessageTransferOptions options3 = null;

            var msgSink4Field = 0;
            var msgSink4 = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (byte[] data, int offset, int length,
                    object fallbackPayload, bool isMoreExpected) =>
                {
                    msgSink4Field++;
                    if (msgSink4Field == 1)
                    {
                        return new ConfigurableMessageSinkResult
                        {
                            Delays = new int[] { 2, 3 },
                        };
                    }
                    else if (msgSink4Field == 2)
                    {
                        return new ConfigurableMessageSinkResult
                        {
                            Delays = new int[] { 1 },
                        };
                    }
                    else
                    {
                        return new ConfigurableMessageSinkResult
                        {
                            Delays = new int[] { 0, 2, 3 },
                        };
                    }
                }
            };
            var options4 = new DefaultMessageTransferOptions
            {
                TimeoutMillis = 60
            };

            var sinkFactoryField = 0;
            var sinkFactory = new ConfigurableMessageSinkFactory
            {
                EventLoop = testEventLoop,
                Logger = logger,
                CreateMessageSinkCallbackInstance = () =>
                {
                    sinkFactoryField++;
                    if (sinkFactoryField == 1)
                    {
                        return new ConfigurableSinkCreationResult
                        {
                            Delays = new int[] { 1 },
                            Sink = msgSink1
                        };
                    }
                    else
                    {
                        return new ConfigurableSinkCreationResult
                        {
                            Delays = new int[] { 2, 2, 3 },
                            Sink = msgSink2
                        };
                    }
                }
            };

            instance.MessageSinkFactory = sinkFactory;

            // act
            testEventLoop.ScheduleTimeout(2, _ =>
            {
                instance.OnReceiveFirstChunk("accra", 0, 1, null, 0, 0, "tea");
            }, null);
            testEventLoop.ScheduleTimeout(2, _ =>
            {
                instance.OnReceiveFirstChunk(null, 64, 2, new byte[] { (byte)'i', (byte)'c', (byte)'e' }, 0, 3, null);
            }, null);
            testEventLoop.ScheduleTimeout(7, _ =>
            {
                instance.OnReceiveSubsequentChunk("aflao", 0, 2, new byte[] { 0, (byte)' ', (byte)'c', (byte)'u', (byte)'p', 0 },
                    1, 4, null);
            }, null);
            testEventLoop.ScheduleTimeout(12, _ =>
            {
                instance.OnReceiveSubsequentChunk(null, 0, 2, null, 1, 4, "s");
            }, null);
            /*testEventLoop.ScheduleTimeout(14, _ =>
            {
                instance.BeginReceive(msgSink3, 7, options3, commonCb, null);
            }, null);
            testEventLoop.ScheduleTimeout(15, _ =>
            {
                instance.BeginSendStartedAtReceiver("1256", msgSink3, 7, options3, commonCb, "y");
            }, null);
            testEventLoop.ScheduleTimeout(15, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 128, 2, 0);
            }, null);
            testEventLoop.ScheduleTimeout(16, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 0, 2, 0);
            }, null);
            testEventLoop.ScheduleTimeout(17, _ =>
            {
                instance.BeginSendStartedAtReceiver(null, msgSink4, 8, options4, commonCb, null);
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
            testEventLoop.ScheduleTimeout(28, _ =>
            {
                instance.OnReceiveSubsequentChunkAck(null, 0, 8, 0);
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
            }, null);*/


            testEventLoop.AdvanceTimeBy(100);

            // assert
            Assert.Equal(0, testEventLoop.PendingEventCount);

            var expectedLogs = new List<string>();
            expectedLogs.Add("2:" +
                OutputEventLogger.CreateSinkCreationLog());
            expectedLogs.Add("2:" +
                OutputEventLogger.CreateSinkCreationLog());
            expectedLogs.Add("3:" +
                OutputEventLogger.CreateSinkWriteDataLog(null, "tea", false));
            expectedLogs.Add("4:" +
                OutputEventLogger.CreateSinkWriteDataLog("ice", null, true));
            expectedLogs.Add("5:" +
                OutputEventLogger.CreateSinkOnEndWriteLog(null)); // msg1
            expectedLogs.Add("5:" +
                OutputEventLogger.CreateOnReceivePduLog("accra", DefaultProtocolDataUnit.Version01,
                DefaultProtocolDataUnit.PduTypeFirstChunkAck, 0, 0, 1, null, null, null));
            expectedLogs.Add("6:" +
                OutputEventLogger.CreateOnReceivePduLog(null, DefaultProtocolDataUnit.Version01,
                DefaultProtocolDataUnit.PduTypeFirstChunkAck, 0, 0, 2, null, null, null));
            expectedLogs.Add("7:" +
                OutputEventLogger.CreateSinkWriteDataLog(" cup", null, true));
            expectedLogs.Add("8:" +
                OutputEventLogger.CreateOnReceivePduLog("aflao", DefaultProtocolDataUnit.Version01,
                DefaultProtocolDataUnit.PduTypeSubsequentChunkAck, 0, 0, 2, null, null, null));
            expectedLogs.Add("12:" +
                OutputEventLogger.CreateSinkWriteDataLog(null, "s", false));
            expectedLogs.Add("12:" +
                OutputEventLogger.CreateSinkOnEndWriteLog(null)); // msg2
            expectedLogs.Add("12:" +
                OutputEventLogger.CreateOnReceivePduLog(null, DefaultProtocolDataUnit.Version01,
                DefaultProtocolDataUnit.PduTypeSubsequentChunkAck, 0, 0, 2, null, null, null));

            logger.AssertEqual(expectedLogs, outputHelper);
        }
    }
}
