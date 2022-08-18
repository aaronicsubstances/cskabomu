using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using Kabomu.Mediator.RequestParsing;
using Kabomu.Mediator.ResponseRendering;
using Kabomu.QuasiHttp.EntityBody;
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

        public IContextRequest Request { get; set; }
        public IContextResponse Response { get; set; }
        public IList<Handler> InitialHandlers { get; set; }
        public IRegistry InitialReadonlyLocalRegistry { get; set; }
        public IRegistry ReadonlyGlobalRegistry { get; set; }
        public IMutexApi MutexApi { get; set; }

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
            if (InitialHandlers == null || InitialHandlers.Count == 0)
            {
                throw new MissingDependencyException("no initial handlers provided");
            }

            var additionalRegistry = new DefaultMutableRegistry();
            additionalRegistry.Add(ContextUtils.TypePatternContext,
                this);
            additionalRegistry.Add(ContextUtils.TypePatternRequest,
                Request);
            additionalRegistry.Add(ContextUtils.TypePatternResponse,
                Response);
            additionalRegistry.Add(ContextUtils.TypePatternPathMatchResult,
                CreateRootPathMatch());

            using (await MutexApi.Synchronize())
            {
                _handlerStack = new Stack<HandlerGroup>();
                var firstHandlerGroup = new HandlerGroup(InitialHandlers, InitialReadonlyLocalRegistry);
                _handlerStack.Push(firstHandlerGroup);
                if (CurrentRegistry == null)
                {
                    _handlerStack.Peek().registry = EmptyRegistry.Instance;
                }

                _joinedRegistry = new DynamicRegistry(this);
                if (ReadonlyGlobalRegistry != null)
                {
                    _joinedRegistry = _joinedRegistry.Join(ReadonlyGlobalRegistry);
                }
                _joinedRegistry = _joinedRegistry.Join(additionalRegistry);

                RunNext();
            }
        }

        private IPathMatchResult CreateRootPathMatch()
        {
            var pathMatchResult = new DefaultPathMatchResult
            {
                PathValues = new Dictionary<string, string>(),
                BoundPathPortion = "",
                UnboundPathPortion = Request.Target ?? ""
            };
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
            if (handlers == null || handlers.Count == 0)
            {
                throw new ArgumentException("no handlers provided", nameof(handlers));
            }

            using (await MutexApi.Synchronize())
            {
                var applicableRegistry = CurrentRegistry;
                if (registry != null)
                {
                    applicableRegistry = CurrentRegistry.Join(registry);
                }
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
                    // calls to CurrentRegistry always succeed.
                    if (_handlerStack.Count == 0)
                    {
                        break;
                    }
                    _handlerStack.Pop();
                }
            }


            if (handler == null)
            {
                _ = HandleUnexpectedEnd();
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
                    await HandleError(new HandlerException(null, e));
                }
                else
                {
                    await HandleError(e);
                }
            }
        }

        public IPathMatchResult PathMatchResult
        {
            get
            {
                return _joinedRegistry.Get<IPathMatchResult>(ContextUtils.TypePatternPathMatchResult);
            }
        }

        public async Task<T> ParseRequest<T>(object parseOpts)
        {
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task<T>)> parserCode = (obj) =>
                {
                    var parser = obj as IRequestParser;
                    if (parser != null && parser.CanParse<T>(this, parseOpts))
                    {
                        var result = parser.Parse<T>(this, parseOpts);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task<T> parserTask;
                using (await MutexApi.Synchronize())
                {
                    bool found;
                    (found, parserTask) = _joinedRegistry.TryGetFirst(
                        ContextUtils.TypePatternRequestParser, parserCode);
                    if (!found)
                    {
                        throw new ParseException("no parser found");
                    }
                }
                return await parserTask;
            }
            catch (Exception e)
            {
                if (e is ParseException)
                {
                    throw;
                }
                else
                {
                    throw new ParseException(null, e);
                }
            }
        }

        public async Task RenderResponse(object body)
        {
            try
            {
                if (body is IRenderable renderable)
                {
                    await renderable.Render(this);
                    return;
                }
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> renderingCode = (obj) =>
                {
                    var renderer = obj as IResponseRenderer;
                    if (renderer != null && renderer.CanRender(this, body))
                    {
                        var result = renderer.Render(this, body);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task renderTask;
                using (await MutexApi.Synchronize())
                {
                    bool found;
                    (found, renderTask) = _joinedRegistry.TryGetFirst(
                        ContextUtils.TypePatternResponseRenderer, renderingCode);
                    if (!found)
                    {
                        throw new RenderException("no renderer found");
                    }
                }
                await renderTask;
            }
            catch (Exception e)
            {
                if (e is RenderException)
                {
                    throw;
                }
                else
                {
                    throw new RenderException(null, e);
                }
            }
        }

        public async Task HandleUnexpectedEnd()
        {
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> unexpectedEndCode = (obj) =>
                {
                    var handler = obj as IUnexpectedEndHandler;
                    if (handler != null)
                    {
                        var result = handler.HandleUnexpectedEnd(this);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task unexpectedEndTask;
                using (await MutexApi.Synchronize())
                {
                    bool found;
                    (found, unexpectedEndTask) = _joinedRegistry.TryGetFirst(
                        ContextUtils.TypePatternUnexpectedEndHandler, unexpectedEndCode);
                    if (!found)
                    {
                        unexpectedEndTask = HandleUnexpectedEndLastResort();
                    }
                }
                await unexpectedEndTask;
            }
            catch (Exception e)
            {
                if (!(e is HandlerException))
                {
                    await HandleError(new HandlerException(null, e));
                }
                else
                {
                    await HandleError(e);
                }
            }
        }

        private Task HandleUnexpectedEndLastResort()
        {
            return Response.SetStatusCode(404).TrySend();
        }

        public async Task HandleError(Exception error)
        {
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> errorHandlingCode = (obj) =>
                {
                    var handler = obj as IServerErrorHandler;
                    if (handler != null)
                    {
                        var result = handler.HandleError(this, error);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task resultTask;
                using (await MutexApi.Synchronize())
                {
                    bool found;
                    (found, resultTask) = _joinedRegistry.TryGetFirst(
                        ContextUtils.TypePatternServerErrorHandler, errorHandlingCode);
                    if (!found)
                    {
                        resultTask = HandleErrorLastResort(error, null);
                    }
                }
                await resultTask;
            }
            catch (Exception e)
            {
                try
                {
                    await HandleErrorLastResort(error, e);
                }
                catch (Exception) { }
            }
        }

        private async Task HandleErrorLastResort(Exception original, Exception errorHandlerException)
        {
            Task sendTask;
            using (await MutexApi.Synchronize())
            {
                string msg;
                if (errorHandlerException != null)
                {
                    msg = "Exception thrown by error handler while handling exception\n" +
                        "Original exception: " + FlattenException(original) + "\n" +
                        "Error handler exception: " + FlattenException(errorHandlerException);
                }
                else
                {
                    msg = FlattenException(original);
                }

                sendTask = Response.SetStatusCode(500)
                    .TrySendWithBody(new StringBody(msg) { ContentType = "text/plain" });
            }
            await sendTask;
        }

        internal static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
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
