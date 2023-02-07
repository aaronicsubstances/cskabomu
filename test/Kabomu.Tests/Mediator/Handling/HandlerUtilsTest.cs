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
            AssertSameHandlers(handlers, context.HandlersSeen);
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
            AssertSameHandlers(handlers, context.HandlersSeen);
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
            AssertSameHandlers(handlers, context.HandlersSeen);
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
            AssertSameHandlers(handlers, context.HandlersSeen);
        }

        [Fact]
        public void TestPathForErrors()
        {
            var handlers = new Handler[]
            {
                context => Task.CompletedTask
            };
            Assert.Throws<ArgumentNullException>(() => HandlerUtils.Path(null, handlers));
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
            var handler = HandlerUtils.Path(pathTemplate, handlers);
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
            AssertSameHandlers(handlers, context.HandlersSeen);
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
            var handler = HandlerUtils.Path(pathTemplate, handlers);
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
        public async Task TestPathForGeneratedTemplateMatch()
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
            var pathTemplateGenerator = new TempPathTemplateGenerator
            {
                PathTemplateToReturn = pathTemplate
            };
            var delegateRegistry = new DefaultMutableRegistry()
                .Add(ContextUtils.RegistryKeyPathTemplateGenerator, pathTemplateGenerator)
                .Add(ContextUtils.RegistryKeyPathMatchResult, prevPathTemplateResult);
            var context = new TempContext
            {
                DelegateRegistry = delegateRegistry
            };
            string spec = "d";
            object options = "k";
            var handler = HandlerUtils.Path(delegateRegistry, spec, options, handlers);
            await handler.Invoke(context);
            Assert.Equal(1, pathTemplateGenerator.ParseCallCount);
            Assert.Equal(spec, pathTemplateGenerator.SpecSeen);
            Assert.Same(options, pathTemplateGenerator.OptionsSeen);
            Assert.Equal(0, context.NextCallCount);
            Assert.Equal(0, context.SkipInsertCallCount);
            Assert.Equal(1, context.InsertCallCount);
            AssertSameHandlers(handlers, context.HandlersSeen);
            Assert.NotNull(context.RegistrySeen);
            Assert.Same(pathTemplate.PathMatchResultToReturn,
                context.RegistrySeen.Get(ContextUtils.RegistryKeyPathMatchResult));
        }

        [Fact]
        public async Task TestPathForGeneratedTemplateMismatch()
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
            var pathTemplateGenerator = new TempPathTemplateGenerator
            {
                PathTemplateToReturn = pathTemplate
            };
            var delegateRegistry = new DefaultMutableRegistry()
                .Add(ContextUtils.RegistryKeyPathTemplateGenerator, pathTemplateGenerator)
                .Add(ContextUtils.RegistryKeyPathMatchResult, prevPathTemplateResult);
            string spec = "de";
            object options = null;
            var handler = HandlerUtils.Path(delegateRegistry, spec, options, handlers);
            var context = new TempContext
            {
                DelegateRegistry = delegateRegistry
            };
            await handler.Invoke(context);
            Assert.Equal(1, pathTemplateGenerator.ParseCallCount);
            Assert.Equal(spec, pathTemplateGenerator.SpecSeen);
            Assert.Same(options, pathTemplateGenerator.OptionsSeen);
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
            AssertSameHandlers(handlers, context.HandlersSeen);
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
            AssertSameHandlers(expectedHandlersSeen, context.HandlersSeen);
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

        private static void AssertSameHandlers(IList<Handler> expected, IList<Handler> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        private class TempContext : IContext
        {
            public TempContext()
            {
                Mutex = new object();
            }

            public object Mutex { get; }
            public IContextRequest Request { get; set; }
            public IContextResponse Response { get; set; }
            public IList<Handler> HandlersSeen { get; set; }
            public IRegistry RegistrySeen { get; set; }
            public int InsertCallCount { get; set; }
            public int SkipInsertCallCount { get; set; }
            public int NextCallCount { get; set; }
            public IRegistry DelegateRegistry { get; set; }

            public void Insert(IList<Handler> handlers)
            {
                Insert(handlers, null);
            }

            public void Insert(IList<Handler> handlers, IRegistry registry)
            {
                InsertCallCount++;
                HandlersSeen = handlers;
                RegistrySeen = registry;
            }

            public void SkipInsert()
            {
                SkipInsertCallCount++;
            }

            public void Next()
            {
                Next(null);
            }

            public void Next(IRegistry registry)
            {
                NextCallCount++;
                RegistrySeen = registry;
            }

            public object Get(object key)
            {
                return DelegateRegistry.Get(key);
            }

            public IEnumerable<object> GetAll(object key)
            {
                return DelegateRegistry.GetAll(key);
            }

            public (bool, object) TryGet(object key)
            {
                return DelegateRegistry.TryGet(key);
            }

            public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
            {
                return DelegateRegistry.TryGetFirst(key, transformFunction);
            }
        }

        class TempPathTemplateGenerator : IPathTemplateGenerator
        {
            public string SpecSeen { get; set; }
            public object OptionsSeen { get; set; }
            public int ParseCallCount { get; set; }

            public IPathTemplate PathTemplateToReturn;

            public IPathTemplate Parse(string spec, object options)
            {
                ParseCallCount++;
                SpecSeen = spec;
                OptionsSeen = options;
                return PathTemplateToReturn;
            }
        }

        class TempPathTemplate : IPathTemplate
        {
            public IPathMatchResult PathMatchResultToReturn { get; set; }
            public string RequestTargetToMatch { get; set; }

            public string Interpolate(IContext context, IDictionary<string, string> pathValues, object opaqueOptionObj)
            {
                throw new NotImplementedException();
            }

            public IList<string> InterpolateAll(IContext context, IDictionary<string, string> pathValues, object opaqueOptionObj)
            {
                throw new NotImplementedException();
            }

            public IPathMatchResult Match(IContext context, string requestTarget)
            {
                return requestTarget == RequestTargetToMatch ? PathMatchResultToReturn : null;
            }
        }
    }
}
