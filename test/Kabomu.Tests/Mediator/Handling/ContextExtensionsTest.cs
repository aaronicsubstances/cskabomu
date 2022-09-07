using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Registry;
using Kabomu.Mediator.RequestParsing;
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
        public async Task TestRenderResponse()
        {

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
    }
}
