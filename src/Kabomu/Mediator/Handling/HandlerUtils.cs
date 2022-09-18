using Kabomu.Concurrency;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public static class HandlerUtils
    {
        public static Handler Chain(IList<Handler> handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }
            if (handlers.Count == 1)
            {
                return handlers[0];
            }
            else
            {
                return context =>
                {
                    context.Insert(handlers);
                    return Task.CompletedTask;
                };
            }
        }

        public static Handler Chain(params Handler[] handlers)
        {
            return Chain((IList<Handler>)handlers);
        }

        public static Handler Register(IRegistry registry, params Handler[] handlers)
        {
            return context =>
            {
                context.Insert(handlers, registry);
                return Task.CompletedTask;
            };
        }

        public static Handler Path(IRegistry registry, string spec, object options, params Handler[] handlers)
        {
            var pathTemplate = ContextUtils.ParseUnboundRequestTarget(registry, spec, options);
            return Path(pathTemplate, handlers);
        }

        public static Handler Path(IPathTemplate pathTemplate, params Handler[] handlers)
        {
            if (pathTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathTemplate));
            }
            return async (context) =>
            {
                IPathMatchResult parentPathMatchResult;
                using (await context.MutexApi.Synchronize())
                {
                    parentPathMatchResult = ContextUtils.GetPathMatchResult(context);
                }
                var pathMatchResult = pathTemplate.Match(context, parentPathMatchResult.UnboundRequestTarget);
                if (pathMatchResult != null)
                {
                    var additionalRegistry = new DefaultMutableRegistry()
                        .Add(ContextUtils.RegistryKeyPathMatchResult, pathMatchResult);
                    context.Insert(handlers, additionalRegistry);
                }
                else
                {
                    context.Next();
                }
            };
        }

        public static Handler ByGet(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodGet, handlers);
        }

        public static Handler ByPost(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodPost, handlers);
        }

        public static Handler ByPut(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodPut, handlers);
        }

        public static Handler ByDelete(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodDelete, handlers);
        }

        public static Handler ByMethod(string method, params Handler[] handlers)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            return (context) =>
            {
                if (context.Request.Method == method)
                {
                    context.Insert(handlers);
                }
                else
                {
                    context.Next();
                }
                return Task.CompletedTask;
            };
        }
    }
}
