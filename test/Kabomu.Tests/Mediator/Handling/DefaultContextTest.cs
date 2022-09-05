﻿using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Mediator.Handling
{
    public class DefaultContextTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public DefaultContextTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task TestForErrors()
        {
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest(), requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);

            var handlers = new List<Handler>();
            handlers.Add((context) => Task.CompletedTask);

            var instance = new DefaultContext();

            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                instance.Request = null;
                instance.Response = contextResponse;
                instance.InitialHandlers = handlers;
                return instance.Start();
            });

            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                instance.Request = contextRequest;
                instance.Response = null;
                instance.InitialHandlers = handlers;
                return instance.Start();
            });

            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                instance.Request = contextRequest;
                instance.Response = contextResponse;
                instance.InitialHandlers = null;
                return instance.Start();
            });

            instance.Request = contextRequest;
            instance.Response = contextResponse;
            instance.InitialHandlers = handlers;
            await instance.Start();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return instance.Insert(null);
            });
        }

        [Fact]
        public async Task TestDefaultRegistryValues()
        {
            var logs = new List<string>();
            var handlers = new List<Handler>();
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest(), requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);
            var instance = new DefaultContext
            {
                InitialHandlers = handlers,
                Request = contextRequest,
                Response = contextResponse
            };
            handlers.Add(async (context) =>
            {
                logs.Add("wer34");
            });
            await instance.Start();

            Assert.Same(instance, instance.Get(ContextUtils.RegistryKeyContext));
            Assert.Same(contextRequest, instance.Get(ContextUtils.RegistryKeyRequest));
            Assert.Same(contextResponse, instance.Get(ContextUtils.RegistryKeyResponse));
            Assert.NotNull(instance.Get<DefaultPathTemplateGenerator>(
                ContextUtils.RegistryKeyPathTemplateGenerator));
            var pathMatchResult = instance.Get<IPathMatchResult>(ContextUtils.RegistryKeyPathMatchResult);
            Assert.NotNull(pathMatchResult);
            Assert.Null(pathMatchResult.UnboundRequestTarget);
            Assert.Null(pathMatchResult.BoundPath);
            Assert.Null(pathMatchResult.PathValues);

            var expectedLogs = new List<string> { "wer34" };
            Assert.Equal(expectedLogs, logs);
        }

        [Fact]
        public async Task TestUseOfInitialLocalAndGlobalRegistries()
        {
            var logs = new List<string>();
            var handlers = new List<Handler>();
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest(), requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);
            var initialReadonlyLocalRegistry = new DefaultMutableRegistry()
                .Add(ContextUtils.RegistryKeyPathTemplateGenerator, null)
                .Add(ContextUtils.RegistryKeyPathMatchResult, null);
            var readonlyGlobalRegistry = new DefaultMutableRegistry()
                .Add(ContextUtils.RegistryKeyContext, null)
                .Add(ContextUtils.RegistryKeyRequest, null)
                .Add(ContextUtils.RegistryKeyResponse, null);
            var instance = new DefaultContext
            {
                InitialHandlers = handlers,
                Request = contextRequest,
                Response = contextResponse,
                InitialHandlerVariables = initialReadonlyLocalRegistry,
                HandlerConstants = readonlyGlobalRegistry
            };
            handlers.Add(async (context) =>
            {
                await context.Next();
                logs.Add("1");
            });
            handlers.Add(async (context) =>
            {
                await context.Next();
                logs.Add("2");
            });
            handlers.Add(async (context) =>
            {
                await context.Next();
                logs.Add("3");
            });
            handlers.Add(async (context) =>
            {
                logs.Add("4");
            });
            await instance.Start();

            Assert.Same(instance, instance.Get(ContextUtils.RegistryKeyContext));
            Assert.Same(contextRequest, instance.Get(ContextUtils.RegistryKeyRequest));
            Assert.Same(contextResponse, instance.Get(ContextUtils.RegistryKeyResponse));
            Assert.Null(instance.Get(ContextUtils.RegistryKeyPathMatchResult));
            Assert.Null(instance.Get(ContextUtils.RegistryKeyPathTemplateGenerator));

            var expectedLogs = new List<string> { "4", "3", "2", "1" };
            ComparisonUtils.AssertLogsEqual(expectedLogs, logs, _outputHelper);
        }

        [Fact]
        public async Task TestNextFromSingleHandler()
        {
            var logs = new List<string>();
            var handlers = new List<Handler>();
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest(), requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);
            var instance = new DefaultContext
            {
                InitialHandlers = handlers,
                Request = contextRequest,
                Response = contextResponse
            };
            handlers.Add(async (context) =>
            {
                await context.Next();
                logs.Add("s");
            });
            await instance.Start();

            var expectedLogs = new List<string> { "s" };
            Assert.Equal(expectedLogs, logs);

            Assert.Same(contextResponse.RawResponse, await responseTransmitter.Task);
            Assert.Equal(404, contextResponse.StatusCode);
        }

        [Fact]
        public async Task TestRegistryAdditions()
        {
            var logs = new List<string>();
            var handlers = new List<Handler>();
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest { Target = "/home" }, requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);
            var instance = new DefaultContext
            {
                InitialHandlers = handlers,
                Request = contextRequest,
                Response = contextResponse,
                InitialHandlerVariables = new IndexedArrayBasedRegistry(new object[] { "u", "v" })
            };
            handlers.Add(async (context) =>
            {
                logs.Add("s");
                CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });

                var innerHandlers = new List<Handler>();
                innerHandlers.Add(async (context) =>
                {
                    logs.Add("inner1");
                    CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "b", "v" });

                    var inner2Handlers = new List<Handler>();
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner1a");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "d", "b", "v" });

                        await context.Next(new IndexedArrayBasedRegistry(new object[] { "e", "f" }));
                    });
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner1b");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "f", "d", "b", "v" });

                        // test that this is equivalent to SkipInsert(), and hence additional registry will be ignored.
                        await context.Next(new ErrorBasedMutableRegistry());
                    });

                    await context.Insert(inner2Handlers, new IndexedArrayBasedRegistry(new object[] { "c", "d" }));
                });

                innerHandlers.Add(async (context) =>
                {
                    logs.Add("inner2");
                    CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "b", "v" });

                    var inner2Handlers = new List<Handler>();
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner2a");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "z", "b", "v" });

                        await context.Next(new IndexedArrayBasedRegistry(new object[] { "-", "0" }));
                    });
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner2b");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "0", "z", "b", "v" });

                        await context.Next(new IndexedArrayBasedRegistry(new object[] { "+", "1" }));
                    });
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner2c");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "1", "0", "z", "b", "v" });

                        await context.Next(new IndexedArrayBasedRegistry(new object[] { "+", "2" }));
                    });
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner2d");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "2", "1", "0", "z", "b", "v" });

                        // test that this is equivalent to Next() at this point.
                        await context.SkipInsert();
                    });

                    await context.Insert(inner2Handlers, new IndexedArrayBasedRegistry(new object[] { "y", "z" }));
                });

                innerHandlers.Add(async (context) =>
                {
                    logs.Add("inner3");
                    CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "b", "v" });
                    await context.Response.SetSuccessStatusCode().Send();

                    // test that this is equivalent to Next().
                    await context.Insert(new List<Handler>());
                });

                await context.Insert(innerHandlers, new IndexedArrayBasedRegistry(new object[] { "a", "b" }));
            });
            handlers.Add(async (context) =>
            {
                logs.Add("t");
                CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
            });
            await instance.Start();

            Assert.NotNull(instance.Get<DefaultPathTemplateGenerator>(
                ContextUtils.RegistryKeyPathTemplateGenerator));

            var pathMatchResult = instance.Get<IPathMatchResult>(ContextUtils.RegistryKeyPathMatchResult);
            Assert.Equal("/home", pathMatchResult.UnboundRequestTarget);
            Assert.Equal("", pathMatchResult.BoundPath);
            Assert.Empty(pathMatchResult.PathValues);

            var expectedLogs = new List<string> { "s", "inner1", "inner1a", "inner1b",
                "inner2", "inner2a", "inner2b", "inner2c", "inner2d", "inner3", "t" };
            ComparisonUtils.AssertLogsEqual(expectedLogs, logs, _outputHelper);

            Assert.Same(contextResponse.RawResponse, await responseTransmitter.Task);
            Assert.Equal(200, contextResponse.StatusCode);
        }

        [Fact]
        public async Task TestRegistryAdditionsWithSkipInsert()
        {
            var logs = new List<string>();
            var handlers = new List<Handler>();
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest { Target = "/home" }, requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);
            var instance = new DefaultContext
            {
                MutexApi = new TempMutexApi
                {
                    Logs = logs,
                    LogToUse = "mutex"
                },
                InitialHandlers = handlers,
                Request = contextRequest,
                Response = contextResponse,
                InitialHandlerVariables = new IndexedArrayBasedRegistry(new object[] { "u", "v" }),
                
            };
            handlers.Add(async (context) =>
            {
                logs.Add("s");
                CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });

                var innerHandlers = new List<Handler>();
                innerHandlers.Add(async (context) =>
                {
                    logs.Add("inner1");
                    CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });

                    var inner2Handlers = new List<Handler>();
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner1a");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
                        await context.Next(null);
                    });
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner1b");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
                        await context.Next(null);
                    });
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner1c");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
                        await context.SkipInsert();
                    });
                    inner2Handlers.Add(async (context) =>
                    {
                        logs.Add("inner1d");
                        CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
                        await context.Next(null);
                    });

                    await context.Insert(inner2Handlers, null);
                });

                innerHandlers.Add(async (context) =>
                {
                    logs.Add("inner2");
                    CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
                    await context.SkipInsert();
                });

                innerHandlers.Add(async (context) =>
                {
                    logs.Add("inner3");
                    CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
                    await context.Next(null);
                });

                await context.Insert(innerHandlers, null);
            });
            handlers.Add(async (context) =>
            {
                logs.Add("t");
                CommonRegistryTestRunner.TestReadonlyOps(instance, 1, new List<object> { "v" });
                Assert.True(await context.Response.SetServerErrorStatusCode().TrySend());
            });
            await instance.Start();

            var expectedLogs = new List<string> { "mutex", "s", "mutex", "inner1", "mutex", "inner1a",
                "mutex", "inner1b", "mutex", "inner1c", "mutex", "inner2", "mutex", "t" };
            ComparisonUtils.AssertLogsEqual(expectedLogs, logs, _outputHelper);

            Assert.Same(contextResponse.RawResponse, await responseTransmitter.Task);
            Assert.Equal(500, contextResponse.StatusCode);
        }

        [Fact]
        public async Task TestSkipInLastHandler()
        {
            var logs = new List<string>();
            var handlers = new List<Handler>();
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest(), requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);
            var instance = new DefaultContext
            {
                InitialHandlers = handlers,
                Request = contextRequest,
                Response = contextResponse
            };
            handlers.Add(async (context) =>
            {
                await context.SkipInsert();
                logs.Add("s");
            });
            await instance.Start();

            var expectedLogs = new List<string> { "s" };
            Assert.Equal(expectedLogs, logs);

            Assert.Same(contextResponse.RawResponse, await responseTransmitter.Task);
            Assert.Equal(404, contextResponse.StatusCode);
        }

        [Fact]
        public async Task TestInitialEmptyHandlers()
        {
            var logs = new List<string>();
            var handlers = new List<Handler>();
            var requestEnvironment = new Dictionary<string, object>();
            var contextRequest = new DefaultContextRequest(
                new DefaultQuasiHttpRequest(), requestEnvironment);
            var responseTransmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(
                new DefaultQuasiHttpResponse(), responseTransmitter);
            var instance = new DefaultContext
            {
                InitialHandlers = handlers,
                Request = contextRequest,
                Response = contextResponse
            };
            await instance.Start();

            var expectedLogs = new List<string>();
            Assert.Equal(expectedLogs, logs);

            Assert.Same(contextResponse.RawResponse, await responseTransmitter.Task);
            Assert.Equal(404, contextResponse.StatusCode);
        }

        private class TempMutexApi : IMutexApi, IMutexContextFactory
        {
            public string LogToUse { get; set; }

            public List<string> Logs { get; set; }

            public bool IsExclusiveRunRequired
            {
                get
                {
                    Logs.Add(LogToUse);
                    return false;
                }
            }

            public IDisposable CreateMutexContext()
            {
                return null;
            }

            public void RunExclusively(Action cb)
            {
                throw new NotImplementedException();
            }
        }
    }
}
