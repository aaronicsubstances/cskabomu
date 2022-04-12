using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSinkFactoryTest
    {
        private readonly ITestOutputHelper outputHelper;

        public ConfigurableMessageSinkFactoryTest(ITestOutputHelper outputHelper)
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
            var instance = new ConfigurableMessageSinkFactory
            {
                EventLoop = testEventLoop,
                Logger = logger,
                CreateMessageSinkCallbackInstance = (c) =>
                {
                    var res = new ConfigurableSinkCreationResult
                    {
                        DelayedError = new ArgumentException("error0"),
                        Delays = new int[] { 7, 8, 9, 8, 9 },
                        Sink = new SimpleMessageSink(null),
                        CancellationIndicator = new DefaultCancellationIndicator(),
                        RecvCb = (o, e) => { },
                        RecvCbState = "sf"
                    };
                    return res;
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("7:3,error0,true,false,true,sf");
            expectedLogs.Add("7:,error0,true,false,true,sf");

            expectedLogs.Add("8:3,error0,true,false,true,sf");
            expectedLogs.Add("8:3,error0,true,false,true,sf");
            expectedLogs.Add("8:,error0,true,false,true,sf");
            expectedLogs.Add("8:,error0,true,false,true,sf");

            expectedLogs.Add("9:3,error0,true,false,true,sf");
            expectedLogs.Add("9:3,error0,true,false,true,sf");
            expectedLogs.Add("9:,error0,true,false,true,sf");
            expectedLogs.Add("9:,error0,true,false,true,sf");

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
            var instance = new ConfigurableMessageSinkFactory
            {
                EventLoop = testEventLoop,
                Logger = logger,
                CreateMessageSinkCallbackInstance = (c) =>
                {
                    var res = new ConfigurableSinkCreationResult
                    {
                        Delays = new int[] { 7 },
                        Sink = null
                    };
                    return res;
                }
            };

            // act.
            var expectedLogs = new List<string>();
            PerformTestAction(instance, testEventLoop, logger, 0, expectedLogs);

            // assert
            expectedLogs.Add("7:3,,false,,false,");
            expectedLogs.Add("7:,,false,,false,");

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
            var instance = new ConfigurableMessageSinkFactory
            {
                EventLoop = testEventLoop,
                Logger = logger,
                CreateMessageSinkCallbackInstance = (c) =>
                {
                    var res = new ConfigurableSinkCreationResult
                    {
                        DelayedError = new ArgumentException("error0"),
                        Delays = new int[0],
                        Sink = null
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
            var instance = new ConfigurableMessageSinkFactory
            {
                EventLoop = testEventLoop,
                Logger = logger,
                CreateMessageSinkCallbackInstance = (c) =>
                {
                    var res = new ConfigurableSinkCreationResult
                    {
                        Delays = null,
                        Sink = new SimpleMessageSink(null)
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
        public void TestLogMethods5()
        {
            // arrange
            var testEventLoop = new TestEventLoopApi();
            var logger = new OutputEventLogger
            {
                EventLoop = testEventLoop,
                Logs = new List<string>()
            };
            var instance = new ConfigurableMessageSinkFactory
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

        private void PerformTestAction(ConfigurableMessageSinkFactory instance,
            TestEventLoopApi testEventLoop, OutputEventLogger logger,
            int startTime, List<string> expectedLogs)
        {
            // arrange
            MessageSinkCreationCallback sinkCb = (cbState, error, sink, cancellationIndicator, recvCb, recvCbState) =>
            {
                var log = $"{testEventLoop.CurrentTimestamp}:" +
                    $"{cbState},{error?.Message}," + (sink != null).ToString().ToLower() + "," +
                    (cancellationIndicator != null ? cancellationIndicator.Cancelled.ToString().ToLower() : "") + "," +
                    $"{(recvCb != null).ToString().ToLower()},{recvCbState}";
                logger.Logs.Add(log);
            };

            // act
            testEventLoop.ScheduleTimeout(startTime, _ =>
            {
                instance.CreateMessageSink(null, sinkCb, 3);
                instance.CreateMessageSink("localhost", sinkCb, null);

            }, null);

            testEventLoop.AdvanceTimeTo(100);

            // assert
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSinkCreationLog(null));
            expectedLogs.Add(startTime + ":" +
                OutputEventLogger.CreateSinkCreationLog("localhost"));
        }
    }
}
