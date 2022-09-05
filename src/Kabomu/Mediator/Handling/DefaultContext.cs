using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultContext : IContext
    {
        private Stack<HandlerGroup> _handlerStack;
        private IRegistry _joinedRegistry;

        public IMutexApi MutexApi { get; set; }
        public IContextRequest Request { get; set; } // getter is equivalent to fetching from joined registry
        public IContextResponse Response { get; set; } // getter is equivalent to fetching from joined registry

        public IList<Handler> InitialHandlers { get; set; }
        public IRegistry InitialHandlerVariables { get; set; }
        public IRegistry HandlerConstants { get; set; }

        public async Task Start()
        {
            if (Request == null)
            {
                throw new MissingDependencyException("request");
            }
            if (Response == null)
            {
                throw new MissingDependencyException("response");
            }
            if (InitialHandlers == null)
            {
                throw new MissingDependencyException("null initial handlers");
            }

            var additionalHandlerConstants = new DefaultMutableRegistry();
            additionalHandlerConstants.Add(ContextUtils.RegistryKeyContext,
                this);
            additionalHandlerConstants.Add(ContextUtils.RegistryKeyRequest,
                Request);
            additionalHandlerConstants.Add(ContextUtils.RegistryKeyResponse,
                Response);

            // only add these if they have not being added already
            var fallbackHandlerVariables = new DefaultMutableRegistry();
            if (InitialHandlerVariables == null ||
                !InitialHandlerVariables.TryGet(ContextUtils.RegistryKeyPathTemplateGenerator).Item1)
            {
                fallbackHandlerVariables.Add(ContextUtils.RegistryKeyPathTemplateGenerator,
                    new DefaultPathTemplateGenerator());
            }
            if (InitialHandlerVariables == null ||
                !InitialHandlerVariables.TryGet(ContextUtils.RegistryKeyPathMatchResult).Item1)
            {
                fallbackHandlerVariables.Add(ContextUtils.RegistryKeyPathMatchResult,
                    CreateRootPathMatch());
            }

            using (await MutexApi.Synchronize())
            {
                _handlerStack = new Stack<HandlerGroup>();
                var firstHandlerGroup = new HandlerGroup(InitialHandlers,
                    fallbackHandlerVariables.Join(InitialHandlerVariables));
                _handlerStack.Push(firstHandlerGroup);

                _joinedRegistry = new DynamicRegistry(this).Join(HandlerConstants).Join(additionalHandlerConstants);

                RunNext();
            }
        }

        private IPathMatchResult CreateRootPathMatch()
        {
            var pathMatchResult = new DefaultPathMatchResultInternal();
            if (Request.Target != null)
            {
                pathMatchResult.UnboundRequestTarget = Request.Target;
                pathMatchResult.BoundPath = "";
                pathMatchResult.PathValues = new Dictionary<string, string>();
            }
            return pathMatchResult;
        }

        /// <summary>
        /// NB: must be called from mutual exclusion
        /// </summary>
        private IRegistry CurrentRegistry
        {
            get
            {
                return _handlerStack.Peek().registry;
            }
        }

        public Task Insert(IList<Handler> handlers)
        {
            return Insert(handlers, null);
        }

        public async Task Insert(IList<Handler> handlers, IRegistry registry)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            using (await MutexApi.Synchronize())
            {
                var applicableRegistry = CurrentRegistry.Join(registry);
                var newHandlerGroup = new HandlerGroup(handlers, applicableRegistry);
                _handlerStack.Push(newHandlerGroup);
                RunNext();
            }
        }

        public async Task SkipInsert()
        {
            using (await MutexApi.Synchronize())
            {
                _handlerStack.Peek().EndIteration();
                RunNext();
            }
        }

        public Task Next()
        {
            return Next(null);
        }

        public async Task Next(IRegistry registry)
        {
            if (registry != null)
            {
                _handlerStack.Peek().registry = CurrentRegistry.Join(registry);
            }
            using (await MutexApi.Synchronize())
            {
                RunNext();
            }
        }

        /// <summary>
        /// NB: must be called from mutual exclusion
        /// </summary>
        private void RunNext()
        {
            // tolerate prescence of nulls.
            Handler handler = null;
            while (handler == null)
            {
                var currentHandlerGroup = _handlerStack.Peek();
                if (currentHandlerGroup.HasNext())
                {
                    handler = currentHandlerGroup.Next();
                }
                else
                {
                    // Always ensure first handler group is never popped off the stack, so that
                    // calls to Peek and CurrentRegistry always succeed.
                    if (_handlerStack.Count == 1)
                    {
                        break;
                    }
                    _handlerStack.Pop();
                }
            }


            if (handler == null)
            {
                _ = ContextExtensions.HandleUnexpectedEnd(this);
                return;
            }

            // call without waiting and without mutual exclusion...it is for client to decide
            // whether to employ mutual exclusion.
            _ = ExecuteHandler(handler);
        }

        private async Task ExecuteHandler(Handler handler)
        {
            try
            {
                await handler.Invoke(this);
            }
            catch (Exception e)
            {
                if (!(e is HandlerException))
                {
                    await ContextExtensions.HandleError(this, new HandlerException(null, e));
                }
                else
                {
                    await ContextExtensions.HandleError(this, e);
                }
            }
        }

        public (bool, object) TryGet(object key)
        {
            return _joinedRegistry.TryGet(key);
        }

        public object Get(object key)
        {
            return _joinedRegistry.Get(key);
        }

        public IEnumerable<object> GetAll(object key)
        {
            return _joinedRegistry.GetAll(key);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return _joinedRegistry.TryGetFirst(key, transformFunction);
        }

        private class DynamicRegistry : IRegistry
        {
            private readonly DefaultContext _context;

            public DynamicRegistry(DefaultContext context)
            {
                _context = context;
            }

            public (bool, object) TryGet(object key)
            {
                return _context.CurrentRegistry.TryGet(key);
            }

            public object Get(object key)
            {
                return _context.CurrentRegistry.Get(key);
            }

            public IEnumerable<object> GetAll(object key)
            {
                return _context.CurrentRegistry.GetAll(key);
            }

            public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
            {
                return _context.CurrentRegistry.TryGetFirst(key, transformFunction);
            }
        }

        private class HandlerGroup
        {
            public readonly Handler[] handlers;
            public IRegistry registry;
            private int _nextIndex;

            public HandlerGroup(IList<Handler> handlers, IRegistry registry)
            {
                // make a copy so that any subsequent changes to list of handlers will not affect 
                // expectations.
                this.handlers = handlers.ToArray();
                this.registry = registry;
                _nextIndex = 0;
            }

            public bool HasNext()
            {
                return _nextIndex < handlers.Length;
            }

            public Handler Next()
            {
                return handlers[_nextIndex++];
            }

            public void EndIteration()
            {
                // Ensure HasNext() returns false afterwards.
                _nextIndex = handlers.Length;
            }
        }
    }
}
