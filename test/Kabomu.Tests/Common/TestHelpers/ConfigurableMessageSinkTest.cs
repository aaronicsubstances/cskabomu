using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSinkTest
    {
        private readonly Xunit.Abstractions.ITestOutputHelper outputHelper;

        public ConfigurableMessageSinkTest(Xunit.Abstractions.ITestOutputHelper outputHelper)
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
            var instance = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (data, offset, length, fallbackPayload, isMoreExpected) =>
                {
                    var res = new ConfigurableMessageSinkResult
                    {
                        DelayedError = new ArgumentException("error0"),
                        Delays = new int[] { 7, 8, 9, 8, 9 }
                    };
                    return res;
                },
                WriteEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(20, _ =>
                    {
                        logger.AppendSinkOnEndWriteLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("7:71,error0");
            expectedLogs.Add("7:,error0");

            expectedLogs.Add("8:71,error0");
            expectedLogs.Add("8:71,error0");
            expectedLogs.Add("8:,error0");
            expectedLogs.Add("8:,error0");

            expectedLogs.Add("9:71,error0");
            expectedLogs.Add("9:71,error0");
            expectedLogs.Add("9:,error0");
            expectedLogs.Add("9:,error0");

            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog(null));

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
            var instance = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (data, offset, length, fallbackPayload, isMoreExpected) =>
                {
                    var res = new ConfigurableMessageSinkResult
                    {
                        Delays = new int[] { 7 }
                    };
                    return res;
                },
                WriteEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(20, _ =>
                    {
                        logger.AppendSinkOnEndWriteLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("7:71,");
            expectedLogs.Add("7:,");

            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog(null));

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
            var instance = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (data, offset, length, fallbackPayload, isMoreExpected) =>
                {
                    var res = new ConfigurableMessageSinkResult
                    {
                        DelayedError = new Exception("x"),
                        Delays = new int[0]
                    };
                    return res;
                },
                WriteEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(10, _ =>
                    {
                        logger.AppendSinkOnEndWriteLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 10, expectedLogs);

            // assert
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog(null));

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
            var instance = new ConfigurableMessageSink
            {
                EventLoop = testEventLoop,
                Logger = logger,
                WriteDataChunkCallbackInstance = (data, offset, length, fallbackPayload, isMoreExpected) =>
                {
                    var res = new ConfigurableMessageSinkResult
                    {
                        DelayedError = new Exception("x"),
                        Delays = null
                    };
                    return res;
                },
                WriteEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(10, _ =>
                    {
                        logger.AppendSinkOnEndWriteLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 10, expectedLogs);

            // assert
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSinkOnEndWriteLog(null));

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
            var instance = new ConfigurableMessageSink
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

        private void PerformTestAction(IMessageSink instance,
            TestEventLoopApi testEventLoop, OutputEventLogger logger,
            int startTime, List<string> expectedLogs)
        {
            // arrange.
            MessageSinkCallback sinkCb = (cbState, error) =>
            {
                var log = $"{testEventLoop.CurrentTimestamp}:" +
                    $"{cbState},{error?.Message}";
                logger.Logs.Add(log);
            };

            // act
            testEventLoop.ScheduleTimeout(startTime, _ =>
            {
                instance.OnDataWrite(new byte[] { (byte)'c', (byte)'a', (byte)'b' },
                    0, 3, null, true, sinkCb, 71);
                instance.OnDataWrite(new byte[] { (byte)'d' },
                    0, 0, "t", false, sinkCb, null);
                instance.OnEndWrite(new Exception("90"));
                instance.OnEndWrite(null);

            }, null);

            testEventLoop.AdvanceTimeTo(100);

            // assert
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSinkWriteDataLog("cab", null, true));
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSinkWriteDataLog("", "t", false));
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSinkOnEndWriteLog("90"));
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSinkOnEndWriteLog(null));
        }
    }
}
