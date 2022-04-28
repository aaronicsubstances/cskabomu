using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using Kabomu.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSourceTest
    {
        private readonly ITestOutputHelper outputHelper;

        public ConfigurableMessageSourceTest(ITestOutputHelper outputHelper)
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
            var instance = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    var res = new ConfigurableMessageSourceResult
                    {
                        DelayedError = new ArgumentException("error0"),
                        Delays = new int[] { 7, 8, 9, 8, 9 },
                        Data = new byte[] { 0, (byte)'d', 1 },
                        Offset = 1,
                        Length = 1,
                        HasMore = true
                    };
                    return res;
                },
                ReadEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(20, _ =>
                    {
                        logger.AppendSourceOnEndReadLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("7:71,error0,d,,true");
            expectedLogs.Add("7:,error0,d,,true");

            expectedLogs.Add("8:71,error0,d,,true");
            expectedLogs.Add("8:71,error0,d,,true");
            expectedLogs.Add("8:,error0,d,,true");
            expectedLogs.Add("8:,error0,d,,true");

            expectedLogs.Add("9:71,error0,d,,true");
            expectedLogs.Add("9:71,error0,d,,true");
            expectedLogs.Add("9:,error0,d,,true");
            expectedLogs.Add("9:,error0,d,,true");

            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null));

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
            var instance = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    var res = new ConfigurableMessageSourceResult
                    {
                        Delays = new int[] { 7 },
                        Data = new byte[] { (byte)'c', (byte)'a', (byte)'b' },
                        Length = 3,
                        HasMore = false,
                        FallbackPayload = 4
                    };
                    return res;
                },
                ReadEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(20, _ =>
                    {
                        logger.AppendSourceOnEndReadLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("7:71,,cab,4,false");
            expectedLogs.Add("7:,,cab,4,false");

            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null));

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
            var instance = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    var res = new ConfigurableMessageSourceResult
                    {
                        Delays = new int[] { 7 },
                        HasMore = false,
                        FallbackPayload = "f"
                    };
                    return res;
                },
                ReadEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(20, _ =>
                    {
                        logger.AppendSourceOnEndReadLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("7:71,,,f,false");
            expectedLogs.Add("7:,,,f,false");

            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null));

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
            var instance = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    var res = new ConfigurableMessageSourceResult
                    {
                        DelayedError = new Exception("x"),
                        Delays = new int[0]
                    };
                    return res;
                },
                ReadEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(10, _ =>
                    {
                        logger.AppendSourceOnEndReadLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 10, expectedLogs);

            // assert
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null));

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
            var instance = new ConfigurableMessageSource
            {
                EventLoop = testEventLoop,
                Logger = logger,
                ReadDataChunkCallbackInstance = () =>
                {
                    var res = new ConfigurableMessageSourceResult
                    {
                        DelayedError = new Exception("x"),
                        Delays = null
                    };
                    return res;
                },
                ReadEndCallbackInstance = (error) =>
                {
                    testEventLoop.ScheduleTimeout(10, _ =>
                    {
                        logger.AppendSourceOnEndReadLog(error);
                    }, null);
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 10, expectedLogs);

            // assert
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog("90"));
            expectedLogs.Add("20:" +
                OutputEventLogger.CreateSourceOnEndReadLog(null));

            logger.AssertEqual(expectedLogs, outputHelper);
        }

        [Fact]
        public void TestLogMethods6()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };
            var instance = new ConfigurableMessageSource
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

        private void PerformTestAction(IMessageSource instance,
            TestEventLoopApi testEventLoop, OutputEventLogger logger,
            int startTime, List<string> expectedLogs)
        {
            // arrange.
            MessageSourceCallback sourceCb = (cbState, error, data, offset, length, fallbackPayload, hasMore) =>
            {
                var dataStr = data == null ? "" : ByteUtils.BytesToString(data, offset, length);
                var log = $"{testEventLoop.CurrentTimestamp}:" +
                    $"{cbState},{error?.Message},{dataStr},{fallbackPayload},{hasMore.ToString().ToLower()}";
                logger.Logs.Add(log);
            };

            // act
            testEventLoop.ScheduleTimeout(startTime, _ =>
            {
                instance.OnDataRead(sourceCb, 71);
                instance.OnDataRead(sourceCb, null);
                instance.OnEndRead(new Exception("90"));
                instance.OnEndRead(null);

            }, null);

            testEventLoop.AdvanceTimeTo(100);

            // assert
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSourceReadDataLog());
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSourceReadDataLog());
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSourceOnEndReadLog("90"));
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSourceOnEndReadLog(null));
        }
    }
}
