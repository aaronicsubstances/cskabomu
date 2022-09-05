using Kabomu.Concurrency;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
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
                return context => context.Insert(handlers);
            }
        }

        public static Handler Chain(params Handler[] handlers)
        {
            return Chain((IList<Handler>)handlers);
        }

        public static Handler Register(IRegistry registry, params Handler[] handlers)
        {
            return context => context.Insert(handlers, registry);
        }

        public static Handler Register(IRegistry registry)
        {
            return context => context.Next(registry);
        }

        public static Handler MountPath(IPathTemplate pathTemplate, params Handler[] handlers)
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
                    parentPathMatchResult = ContextExtensions.GetPathMatchResult(context);
                }
                var pathMatchResult = pathTemplate.Match(context, parentPathMatchResult.UnboundRequestTarget);
                if (pathMatchResult != null)
                {
                    var additionalRegistry = new DefaultMutableRegistry()
                        .Add(ContextUtils.RegistryKeyPathMatchResult, pathMatchResult);
                    await context.Insert(handlers, additionalRegistry);
                }
                else
                {
                    await context.Next();
                }
            };
        }
    }
}
