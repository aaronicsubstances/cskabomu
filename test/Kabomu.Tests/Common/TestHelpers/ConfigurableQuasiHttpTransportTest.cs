using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableQuasiHttpTransportTest
    {
        private readonly ITestOutputHelper outputHelper;

        public ConfigurableQuasiHttpTransportTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void TestLogMethods1()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };
            var instance = new ConfigurableQuasiHttpTransport
            {
                EventLoop = testEventLoop,
                Logger = logger,
                SendPduCallbackInstance = (object connectionHandle,
                    byte version, byte pduType, byte flags,
                    byte errorCode, long messageId,
                    byte[] data, int offset, int length, object fallbackPayload,
                    ICancellationIndicator cancellationIndicator) =>
                {
                    var res = new ConfigurableSendPduResult
                    {
                        DelayedError = new ArgumentException("error1"),
                        Delays = new int[] { 1, 2, 1 }
                    };
                    return res;
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("1:7,error1");
            expectedLogs.Add("1:7,error1");
            expectedLogs.Add("1:,error1");
            expectedLogs.Add("1:,error1");
            expectedLogs.Add("2:7,error1");
            expectedLogs.Add("2:,error1");

            logger.AssertEqual(expectedLogs, outputHelper);
        }

        [Fact]
        public void TestLogMethods2()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };
            var instance = new ConfigurableQuasiHttpTransport
            {
                EventLoop = testEventLoop,
                Logger = logger,
                SendPduCallbackInstance = (object connectionHandle,
                    byte version, byte pduType, byte flags,
                    byte errorCode, long messageId,
                    byte[] data, int offset, int length, object fallbackPayload,
                    ICancellationIndicator cancellationIndicator) =>
                {
                    var res = new ConfigurableSendPduResult
                    {
                        Delays = new int[] { 1 }
                    };
                    return res;
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("1:7,");
            expectedLogs.Add("1:,");

            logger.AssertEqual(expectedLogs, outputHelper);
        }

        [Fact]
        public void TestLogMethods3()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };
            var instance = new ConfigurableQuasiHttpTransport
            {
                EventLoop = testEventLoop,
                Logger = logger,
                SendPduCallbackInstance = (object connectionHandle,
                    byte version, byte pduType, byte flags,
                    byte errorCode, long messageId,
                    byte[] data, int offset, int length, object fallbackPayload,
                    ICancellationIndicator cancellationIndicator) =>
                {
                    var res = new ConfigurableSendPduResult
                    {
                        DelayedError = new ArgumentException("error1"),
                        Delays = new int[0]
                    };
                    return res;
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 10, expectedLogs);

            // assert
            logger.AssertEqual(expectedLogs, outputHelper);
        }

        [Fact]
        public void TestLogMethods4()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };
            var instance = new ConfigurableQuasiHttpTransport
            {
                EventLoop = testEventLoop,
                Logger = logger,
                SendPduCallbackInstance = (object connectionHandle,
                    byte version, byte pduType, byte flags,
                    byte errorCode, long messageId,
                    byte[] data, int offset, int length, object fallbackPayload,
                    ICancellationIndicator cancellationIndicator) =>
                {
                    var res = new ConfigurableSendPduResult
                    {
                        Delays = null
                    };
                    return res;
                },
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 10, expectedLogs);

            // assert
            logger.AssertEqual(expectedLogs, outputHelper);
        }

        [Fact]
        public void TestLogMethods5()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };
            var instance = new ConfigurableQuasiHttpTransport
            {
                EventLoop = testEventLoop,
                Logger = logger
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 10, expectedLogs);

            // assert
            logger.AssertEqual(expectedLogs, outputHelper);
        }

        private void PerformTestAction(ConfigurableQuasiHttpTransport instance,
            TestEventLoopApi testEventLoop, OutputEventLogger logger,
            int startTime, List<string> expectedLogs)
        {
            // arrange
            Action<object, Exception> sendChunkCb = (cbState, error) =>
            {
                var log = $"{testEventLoop.CurrentTimestamp}:" +
                    $"{cbState},{error?.Message}";
                logger.Logs.Add(log);
            };

            // act
            testEventLoop.ScheduleTimeout(startTime, _ =>
            {
                instance.BeginSendPdu(null, 1, 2, 3, 4, 5, null, 0, 0, 9, null, sendChunkCb, 7);
                instance.BeginSendPdu("d", 10, 21, 23, 24, 85, new byte[] { (byte)'a', (byte)'f' },
                    0, 2, null, new DefaultCancellationIndicator(), sendChunkCb, null);

            }, null);

            testEventLoop.AdvanceTimeTo(100);

            // assert
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateOnReceivePduLog(null, 1, 2, 3, 4, 5, null, 9, null));
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateOnReceivePduLog("d", 10, 21, 23, 24, 85, "af", null, false));
        }
    }
}
