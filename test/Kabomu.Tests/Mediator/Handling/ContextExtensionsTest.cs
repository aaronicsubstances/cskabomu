﻿using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Registry;
using Kabomu.Mediator.RequestParsing;
using Kabomu.Mediator.ResponseRendering;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Mediator.Handling
{
    public class ContextExtensionsTest
    {
        private static async Task<IContext> CreateAndStartContext(IRegistry initialRegistry)
        {
            var contextRequest = new DefaultContextRequestInternal(
                new DefaultQuasiHttpRequest(), null);
            var contextResponse = new DefaultContextResponseInternal(
                new DefaultQuasiHttpResponse(), new TaskCompletionSource<IQuasiHttpResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously));
            var context = new DefaultContextInternal
            {
                InitialHandlers = new Handler[]
                {
                    _ => Task.CompletedTask
                },
                InitialHandlerVariables = initialRegistry,
                Request = contextRequest,
                Response = contextResponse
            };
            await context.Start();
            return context;
        }

        [Fact]
        public async Task TestParseRequest1()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);

            object parseOpts = null;
            await Assert.ThrowsAsync<NoSuchParserException>(() => 
                ContextExtensions.ParseRequest<int>(context, parseOpts));
        }

        [Fact]
        public async Task TestParseRequest2()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, null);

            object parseOpts = new object();
            await Assert.ThrowsAsync<NoSuchParserException>(() =>
                ContextExtensions.ParseRequest<int>(context, parseOpts));
        }

        [Fact]
        public async Task TestParseRequest3()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, "invalid");

            object parseOpts = new object();
            await Assert.ThrowsAsync<RequestParsingException>(() =>
                ContextExtensions.ParseRequest<string>(context, parseOpts));
        }

        [Fact]
        public async Task TestParseRequest4()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, null);
            var requestParser1 = new TestRequestParser
            {
                CanParseReturnValue = false
            };
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, requestParser1);
            var requestParser2 = new TestRequestParser
            {
                CanParseReturnValue = false
            };
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, requestParser2);
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, null);

            object parseOpts = new object();
            await Assert.ThrowsAsync<NoSuchParserException>(() =>
                ContextExtensions.ParseRequest<string>(context, parseOpts));

            Assert.Equal(1, requestParser1.CanParseCallCount);
            Assert.Same(context, requestParser1.CanParseContextSeen);
            Assert.Same(parseOpts, requestParser1.CanParseParseOptsSeen);
            Assert.Equal(0, requestParser1.ParseCallCount);

            Assert.Equal(1, requestParser2.CanParseCallCount);
            Assert.Same(context, requestParser2.CanParseContextSeen);
            Assert.Same(parseOpts, requestParser2.CanParseParseOptsSeen);
            Assert.Equal(0, requestParser1.ParseCallCount);
        }

        [Fact]
        public async Task TestParseRequest5()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, null);
            var requestParserPickedUp = new TestRequestParser
            {
                CanParseReturnValue = true,
                ParseReturnValue = "done"
            };
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, requestParserPickedUp);
            var requestParserSkipped = new TestRequestParser
            {
                CanParseReturnValue = false
            };
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, requestParserSkipped);
            initialRegistry.Add(ContextUtils.RegistryKeyRequestParser, null);

            object parseOpts = new object();
            var actualReturnValue = await ContextExtensions.ParseRequest<string>(context, parseOpts);

            Assert.Equal(1, requestParserPickedUp.CanParseCallCount);
            Assert.Same(context, requestParserPickedUp.CanParseContextSeen);
            Assert.Same(parseOpts, requestParserPickedUp.CanParseParseOptsSeen);
            Assert.Equal(1, requestParserPickedUp.ParseCallCount);
            Assert.Same(context, requestParserPickedUp.ParseContextSeen);
            Assert.Same(parseOpts, requestParserPickedUp.ParseParseOptsSeen);
            Assert.Equal(requestParserPickedUp.ParseReturnValue, actualReturnValue);

            Assert.Equal(1, requestParserSkipped.CanParseCallCount);
            Assert.Same(context, requestParserSkipped.CanParseContextSeen);
            Assert.Same(parseOpts, requestParserSkipped.CanParseParseOptsSeen);
            Assert.Equal(0, requestParserSkipped.ParseCallCount);
        }

        [Fact]
        public async Task TestRenderResponse1()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);

            object obj = "html";
            await Assert.ThrowsAsync<NoSuchRendererException>(() =>
                ContextExtensions.RenderResponse(context, obj));
        }

        [Fact]
        public async Task TestRenderResponse2()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, null);

            object obj = new object();
            await Assert.ThrowsAsync<NoSuchRendererException>(() =>
                ContextExtensions.RenderResponse(context, obj));
        }

        [Fact]
        public async Task TestRenderResponse3()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, "invalid");

            object obj = new object();
            await Assert.ThrowsAsync<ResponseRenderingException>(() =>
                ContextExtensions.RenderResponse(context, obj));
        }

        [Fact]
        public async Task TestRenderResponse4()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, null);
            var responseRenderer1 = new TestResponseRenderer
            {
                CanRenderReturnValue = false
            };
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, responseRenderer1);
            var responseRenderer2 = new TestResponseRenderer
            {
                CanRenderReturnValue = false
            };
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, responseRenderer2);
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, null);

            object obj = new object();
            await Assert.ThrowsAsync<NoSuchRendererException>(() =>
                ContextExtensions.RenderResponse(context, obj));

            Assert.Equal(1, responseRenderer1.CanRenderCallCount);
            Assert.Same(context, responseRenderer1.CanRenderContextSeen);
            Assert.Same(obj, responseRenderer1.CanRenderObjSeen);
            Assert.Equal(0, responseRenderer1.RenderCallCount);

            Assert.Equal(1, responseRenderer2.CanRenderCallCount);
            Assert.Same(context, responseRenderer2.CanRenderContextSeen);
            Assert.Same(obj, responseRenderer2.CanRenderObjSeen);
            Assert.Equal(0, responseRenderer1.RenderCallCount);
        }

        [Fact]
        public async Task TestRenderResponse5()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, null);
            var responseRendererPickedUp = new TestResponseRenderer
            {
                CanRenderReturnValue = true
            };
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, responseRendererPickedUp);
            var responseRendererSkipped = new TestResponseRenderer
            {
                CanRenderReturnValue = false
            };
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, responseRendererSkipped);
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, null);

            object obj = new object();
            await ContextExtensions.RenderResponse(context, obj);

            Assert.Equal(1, responseRendererPickedUp.CanRenderCallCount);
            Assert.Same(context, responseRendererPickedUp.CanRenderContextSeen);
            Assert.Same(obj, responseRendererPickedUp.CanRenderObjSeen);
            Assert.Equal(1, responseRendererPickedUp.RenderCallCount);
            Assert.Same(context, responseRendererPickedUp.RenderContextSeen);
            Assert.Same(obj, responseRendererPickedUp.RenderObjSeen);

            Assert.Equal(1, responseRendererSkipped.CanRenderCallCount);
            Assert.Same(context, responseRendererSkipped.CanRenderContextSeen);
            Assert.Same(obj, responseRendererSkipped.CanRenderObjSeen);
            Assert.Equal(0, responseRendererSkipped.RenderCallCount);
        }

        [Fact]
        public async Task TestRenderResponse6()
        {
            var initialRegistry = new DefaultMutableRegistry();
            var context = await CreateAndStartContext(initialRegistry);
            var responseRenderer1 = new TestResponseRenderer
            {
                CanRenderReturnValue = true
            };
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, responseRenderer1);
            var responseRenderer2 = new TestResponseRenderer
            {
                CanRenderReturnValue = true
            };
            initialRegistry.Add(ContextUtils.RegistryKeyResponseRenderer, responseRenderer2);

            var obj = new TestRenderable();
            await ContextExtensions.RenderResponse(context, obj);
            Assert.Equal(1, obj.RenderCallCount);
            Assert.Same(context, obj.RenderContextSeen);

            Assert.Equal(0, responseRenderer1.CanRenderCallCount);
            Assert.Equal(0, responseRenderer1.RenderCallCount);

            Assert.Equal(0, responseRenderer2.CanRenderCallCount);
            Assert.Equal(0, responseRenderer2.RenderCallCount);
        }

        [Fact]
        public async Task TestHandleUnexpectedEnd()
        {

        }

        [Fact]
        public async Task TestHandleError()
        {

        }

        private class TestRequestParser : IRequestParser
        {
            public IContext CanParseContextSeen { get; private set; }
            public object CanParseParseOptsSeen { get; private set; }
            public IContext ParseContextSeen { get; private set; }
            public object ParseParseOptsSeen { get; private set; }
            public int CanParseCallCount { get; private set; }
            public int ParseCallCount { get; private set; }
            public bool CanParseReturnValue { get; set; }
            public object ParseReturnValue { get; set; }

            public bool CanParse<T>(IContext context, object parseOpts)
            {
                CanParseCallCount++;
                CanParseContextSeen = context;
                CanParseParseOptsSeen = parseOpts;
                return CanParseReturnValue;
            }

            public Task<T> Parse<T>(IContext context, object parseOpts)
            {
                ParseCallCount++;
                ParseContextSeen = context;
                ParseParseOptsSeen = parseOpts;
                return Task.FromResult((T)ParseReturnValue);
            }
        }

        class TestResponseRenderer : IResponseRenderer
        {
            public IContext RenderContextSeen { get; private set; }
            public object RenderObjSeen { get; private set; }
            public IContext CanRenderContextSeen { get; private set; }
            public object CanRenderObjSeen { get; private set; }
            public int RenderCallCount { get; private set; }
            public int CanRenderCallCount { get; private set; }
            public bool CanRenderReturnValue { get; set; }

            public bool CanRender(IContext context, object obj)
            {
                CanRenderCallCount++;
                CanRenderContextSeen = context;
                CanRenderObjSeen = obj;
                return CanRenderReturnValue;
            }

            public Task Render(IContext context, object obj)
            {
                RenderCallCount++;
                RenderContextSeen = context;
                RenderObjSeen = obj;
                return Task.CompletedTask;
            }
        }

        private class TestRenderable : IRenderable
        {
            public int RenderCallCount { get; internal set; }
            public IContext RenderContextSeen { get; internal set; }

            public Task Render(IContext context)
            {
                RenderCallCount++;
                RenderContextSeen = context;
                return Task.CompletedTask;
            }
        }
    }
}
