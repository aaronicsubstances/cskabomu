using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Mediator.Handling
{
    public class HandlerUtilsTest
    {
        [Fact]
        public void TestChainForSingleArgument()
        {
            Handler handler = context => Task.CompletedTask;
            Assert.Same(handler, HandlerUtils.Chain(handler));
            Assert.Same(handler, HandlerUtils.Chain(new List<Handler> { handler }));
        }

        [Fact]
        public async Task TestChainForNonSingleArguments1()
        {
            var handlers = new Handler[0];
            var handler = HandlerUtils.Chain(handlers);
            var context = new TempContext();
            await handler.Invoke(context);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            Assert.Null(context.RegistrySeen);
            // check sameness of items
            Assert.Equal(handlers, context.HandlersSeen);
        }

        [Fact]
        public async Task TestChainForNonSingleArguments2()
        {
            var handlers = new List<Handler>();
            var handler = HandlerUtils.Chain(handlers);
            var context = new TempContext();
            await handler.Invoke(context);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            Assert.Null(context.RegistrySeen);
            Assert.Equal(handlers, context.HandlersSeen);
        }

        [Fact]
        public async Task TestChainForNonSingleArguments3()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask,
                context => Task.CompletedTask,
            };
            var handler = HandlerUtils.Chain(handlers);
            var context = new TempContext();
            await handler.Invoke(context);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            Assert.Null(context.RegistrySeen);
            Assert.Equal(handlers, context.HandlersSeen);
        }

        [Fact]
        public async Task TestChainForNonSingleArguments4()
        {
            var handlers = new List<Handler>
            {
                context => Task.CompletedTask,
                context => Task.CompletedTask,
                context => Task.CompletedTask,
            };
            var handler = HandlerUtils.Chain(handlers);
            var context = new TempContext();
            await handler.Invoke(context);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            Assert.Null(context.RegistrySeen);
            Assert.Equal(handlers, context.HandlersSeen);
        }

        [Fact]
        public void TestPathForErrors()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask
            };
            Assert.Throws<ArgumentNullException>(() => HandlerUtils.ByPath(null, handlers));
        }

        [Fact]
        public async Task TestPathForExistingTemplateMatch()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask
            };
            var prevPathTemplateResult = new DefaultPathMatchResultInternal
            {
                UnboundRequestTarget = "/"
            };
            var pathTemplate = new TempPathTemplate
            {
                PathMatchResultToReturn = new DefaultPathMatchResultInternal(),
                RequestTargetToMatch = "/"
            };
            var handler = HandlerUtils.ByPath(pathTemplate, handlers);
            var delegateRegistry = new DefaultMutableRegistry()
                .Add(ContextUtils.RegistryKeyPathMatchResult, prevPathTemplateResult);
            var context = new TempContext
            {
                DelegateRegistry = delegateRegistry
            };
            await handler.Invoke(context);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            Assert.Equal(handlers, context.HandlersSeen);
            Assert.NotNull(context.RegistrySeen);
            Assert.Same(pathTemplate.PathMatchResultToReturn,
                context.RegistrySeen.Get(ContextUtils.RegistryKeyPathMatchResult));
        }

        [Fact]
        public async Task TestPathForExistingTemplateMismatch()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask
            };
            var prevPathTemplateResult = new DefaultPathMatchResultInternal
            {
                UnboundRequestTarget = "/tree"
            };
            var pathTemplate = new TempPathTemplate
            {
                PathMatchResultToReturn = new DefaultPathMatchResultInternal(),
                RequestTargetToMatch = "/moon"
            };
            var handler = HandlerUtils.ByPath(pathTemplate, handlers);
            var delegateRegistry = new DefaultMutableRegistry()
                .Add(ContextUtils.RegistryKeyPathMatchResult, prevPathTemplateResult);
            var context = new TempContext
            {
                DelegateRegistry = delegateRegistry
            };
            await handler.Invoke(context);
            Assert.Equal(1, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(0, context.InsertCallCount);
            Assert.Null(context.HandlersSeen);
            Assert.Null(context.RegistrySeen);
        }

        [Fact]
        public async Task TestRegister()
        {
            IRegistry registry = new DefaultMutableRegistry();
            var handler = HandlerUtils.Register(registry);
            var context = new TempContext();
            await handler.Invoke(context);
            Assert.Equal(1, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(0, context.InsertCallCount);
            Assert.Same(registry, context.RegistrySeen);
        }

        [Fact]
        public async Task TestRegisterWithNull()
        {
            var handler = HandlerUtils.Register(null);
            var context = new TempContext();
            await handler.Invoke(context);
            Assert.Equal(1, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(0, context.InsertCallCount);
            Assert.Null(context.RegistrySeen);
            Assert.Null(context.HandlersSeen);
        }

        [Fact]
        public async Task TestRegisterWithHandlers()
        {
            var handlers = new Handler[]
            {
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
            };
            var registry = new DecrementingCounterBasedRegistry();
            var handler = HandlerUtils.Register(registry, handlers);
            var context = new TempContext();
            await handler.Invoke(context);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            Assert.Same(registry, context.RegistrySeen);
            Assert.Equal(handlers, context.HandlersSeen);
        }

        [Fact]
        public void TestByMethodMatchForErrors()
        {
            var handlers = new Handler[]
            {
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
            };
            Assert.Throws<ArgumentNullException>(() =>
                HandlerUtils.ByMethod(null, handlers));
        }

        [Fact]
        public Task TestByMethodMatch()
        {
            var handlers = new Handler[]
            {
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
            };
            var handler = HandlerUtils.ByMethod("TRACE", handlers);
            return TestCommonByMethodMatch(handler, "TRACE", handlers);
        }

        [Fact]
        public Task TestByMethodMismatch()
        {
            var handlers = new Handler[]
            {
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
            };
            var handler = HandlerUtils.ByMethod("TRACE", handlers);
            return TestCommonByMethodMismatch(handler, "HEAD");
        }

        [Fact]
        public Task TestByGetMatch()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask
            };
            var handler = HandlerUtils.ByGet(handlers);
            return TestCommonByMethodMatch(handler, "GET", handlers);
        }

        [Fact]
        public Task TestByGetMismatch()
        {
            var handlers = new Handler[]
            {
                null, null
            };
            var handler = HandlerUtils.ByGet(handlers);
            return TestCommonByMethodMismatch(handler, "POST");
        }

        [Fact]
        public Task TestByPostMatch()
        {
            var handlers = new Handler[0];
            var handler = HandlerUtils.ByPost(handlers);
            return TestCommonByMethodMatch(handler, "POST", handlers);
        }

        [Fact]
        public Task TestByPostMismatch()
        {
            var handlers = new Handler[] { null };
            var handler = HandlerUtils.ByGet(handlers);
            return TestCommonByMethodMismatch(handler, "PUT");
        }

        [Fact]
        public Task TestByPutMatch()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask,
                context => Task.CompletedTask,
            };
            var handler = HandlerUtils.ByPut(handlers);
            return TestCommonByMethodMatch(handler, "PUT", handlers);
        }

        [Fact]
        public Task TestByPutMismatch()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask,
                context => Task.CompletedTask,
            };
            var handler = HandlerUtils.ByPut(handlers);
            return TestCommonByMethodMismatch(handler, "GET");
        }

        [Fact]
        public Task TestByDeleteMatch()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask,
                context => Task.CompletedTask,
            };
            var handler = HandlerUtils.ByDelete(handlers);
            return TestCommonByMethodMatch(handler, "DELETE", handlers);
        }

        [Fact]
        public Task TestByDeleteMismatch()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask,
                context => Task.CompletedTask,
            };
            var handler = HandlerUtils.ByDelete(handlers);
            return TestCommonByMethodMismatch(handler, "PUT");
        }

        private async Task TestCommonByMethodMatch(Handler handler, string methodToMatch,
            Handler[] expectedHandlersSeen)
        {
            var rawRequest = new DefaultQuasiHttpRequest
            {
                Method = methodToMatch
            };
            var contextRequest = new DefaultContextRequestInternal(rawRequest);
            var context = new TempContext
            {
                Request = contextRequest
            };
            await handler.Invoke(context);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            Assert.Null(context.RegistrySeen);
            Assert.Equal(expectedHandlersSeen, context.HandlersSeen);
        }

        private async Task TestCommonByMethodMismatch(Handler handler, string methodToMismatch)
        {
            var handlers = new Handler[]
            {
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
                (context) => Task.CompletedTask,
            };
            var rawRequest = new DefaultQuasiHttpRequest
            {
                Method = methodToMismatch
            };
            var contextRequest = new DefaultContextRequestInternal(rawRequest);
            var context = new TempContext
            {
                Request = contextRequest
            };
            await handler.Invoke(context);
            Assert.Equal(1, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(0, context.InsertCallCount);
            Assert.Null(context.RegistrySeen);
            Assert.Null(context.HandlersSeen);
        }
    }
}
