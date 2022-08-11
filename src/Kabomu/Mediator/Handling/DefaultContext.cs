using Kabomu.Mediator.Registry;
using Kabomu.Mediator.RequestParsing;
using Kabomu.Mediator.ResponseRendering;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultContext : IContext
    {
        private Stack<HandlerGroup> _handlerStack;
        private IRegistry _joinedRegistry;

        public IRequest Request { get; set; }
        public IResponse Response { get; set; }
        public IRegistry InitialReadonlyLocalRegistry { get; set; }
        public IRegistry ReadonlyGlobalRegistry { get; set; }
        public IList<Handler> InitialHandlers { get; set; }
        public Handler FinalHandler { get; set; }

        public async Task Start()
        {
            _handlerStack = new Stack<HandlerGroup>();
            var firstHandlerGroup = new HandlerGroup(InitialHandlers, InitialReadonlyLocalRegistry);
            _handlerStack.Push(firstHandlerGroup);

            _joinedRegistry = new DynamicRegistry(this).Join(ReadonlyGlobalRegistry);

            try
            {
                await Next();
            }
            catch (Exception e)
            {
                await HandleError(e);
            }
        }

        private IRegistry CurrentRegistry
        {
            get
            {
                return _handlerStack.Peek().registry;
            }
        }

        public Task Insert(IList<Handler> handlers)
        {
            if ((handlers?.Count ?? 0) == 0)
            {
                throw new ArgumentException("no handlers provided", nameof(handlers));
            }

            var newHandlerGroup = new HandlerGroup(handlers, CurrentRegistry);
            _handlerStack.Push(newHandlerGroup);
            return Next();
        }

        public Task Insert(IRegistry registry, IList<Handler> handlers)
        {
            if ((handlers?.Count ?? 0) == 0)
            {
                throw new ArgumentException("no handlers provided", nameof(handlers));
            }

            var newHandlerGroup = new HandlerGroup(handlers, CurrentRegistry.Join(registry));
            _handlerStack.Push(newHandlerGroup);
            return Next();
        }

        public Task Next(IRegistry registry)
        {
            _handlerStack.Peek().registry = CurrentRegistry.Join(registry);
            return Next();
        }

        public async Task Next()
        {
            Handler handler = null;
            // allow null handlers to be inserted.
            while (_handlerStack.Count > 0 && handler == null)
            {
                var currentHandlerGroup = _handlerStack.Peek();
                if (currentHandlerGroup.HasNext())
                {
                    handler = currentHandlerGroup.Next();
                }
                else
                {
                    _handlerStack.Pop();
                }
            }

            if (handler == null)
            {
                handler = FinalHandler;
            }

            try
            {
                await handler.Invoke(this);
            }
            catch (Exception e)
            {
                if (e is HandlerException)
                {
                    throw;
                }
                else
                {
                    throw new HandlerException(e);
                }
            }
        }

        public IPathBinding PathBinding
        {
            get
            {
                return _joinedRegistry.Get<IPathBinding>();
            }
        }

        public Task<T> ParseRequest<T>(object parseOpts)
        {
            // allow null parsers to be inserted.
            Func<IRequestParser, (bool, Task<T>)> parserCode = (parser) =>
            {
                if (parser != null && parser.CanParse<T>(this, parseOpts))
                {
                    var result = parser.Parse<T>(this, parseOpts);
                    return (true, result);
                }
                return (false, null);
            };
            var (found, parserTask) = _joinedRegistry.TryGetFirst(parserCode);
            if (!found)
            {
                throw new NoSuchParserException();
            }
            else
            {
                return parserTask;
            }
        }

        public Task RenderResponse(object body)
        {
            if (body is IRenderable renderable)
            {
                return renderable.Render(this);
            }
            Func<IRenderer, (bool, Task)> renderingCode = (renderer) =>
            {
                if (renderer != null && renderer.CanRender(this, body))
                {
                    var result = renderer.Render(this, body);
                    return (true, result);
                }
                return (false, null);
            };
            var (found, renderTask) = _joinedRegistry.TryGetFirst(renderingCode);
            if (!found)
            {
                throw new NoSuchRendererException();
            }
            else
            {
                return renderTask;
            }
        }

        public async Task HandleError(Exception error)
        {
            try
            {
                var (found, errorHandler) = _joinedRegistry.TryGet<IServerErrorHandler>();
                if (found)
                {
                    await errorHandler.HandleError(this, error);
                }
                else
                {
                    await HandleErrorLastResort(error, null);
                }
            }
            catch (Exception e)
            {
                await HandleErrorLastResort(error, e);
            }
        }

        private async Task HandleErrorLastResort(Exception original, Exception errorHandlerException)
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

            try
            {
                await Response.SetStatusIndicatesSuccess(false)
                    .SetStatusIndicatesClientError(false)
                    .SendWithBody(new StringBody(msg) { ContentType = "text/plain" });
            }
            catch (Exception) { }
        }

        private static string FlattenException(Exception exception)
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
            public readonly IList<Handler> handlers;
            public IRegistry registry;
            private int _nextIndex;

            public HandlerGroup(IList<Handler> handlers, IRegistry registry)
            {
                this.handlers = handlers;
                this.registry = registry;
                _nextIndex = 0;
            }

            public bool HasNext()
            {
                return _nextIndex < handlers.Count;
            }

            public Handler Next()
            {
                return handlers[_nextIndex++];
            }
        }
    }
}
