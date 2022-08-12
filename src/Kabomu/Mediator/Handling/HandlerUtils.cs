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
            if (handlers == null || handlers.Count == 0)
            {
                return context =>
                {
                    context.Next();
                    return Task.CompletedTask;
                };
            }
            if (handlers.Count == 1)
            {
                return handlers[0];
            }
            Handler chainHandler = context =>
            {
                context.Insert(handlers);
                return Task.CompletedTask;
            };
            return chainHandler;
        }

        public static Handler Register(IRegistry registry, Handler handler)
        {
            return context =>
            {
                context.Insert(new List<Handler> { handler }, registry);
                return Task.CompletedTask;
            };
        }

        public static Handler Register(IRegistry registry)
        {
            return context =>
            {
                context.Next(registry);
                return Task.CompletedTask;
            };
        }
    }
}
