﻿using Kabomu.Concurrency;
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

        public static Handler Path(string path, Handler handler)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            var pathBinder = DefaultPathParser.Parse(path);
            return Path(pathBinder, handler);
        }

        public static Handler Path(IPathMatcher pathMatcher, Handler handler)
        {
            if (pathMatcher == null)
            {
                throw new ArgumentNullException(nameof(pathMatcher));
            }
            return async (context) =>
            {
                IPathMatchResult parentPathMatcher;
                using (await context.MutexApi.Synchronize())
                {
                    parentPathMatcher = context.PathMatchResult;
                }
                var pathMatchResult = pathMatcher.Match(parentPathMatcher.UnboundPathPortion);
                if (pathMatchResult != null)
                {
                    var additionalRegistry = new DefaultMutableRegistry()
                        .Add(ContextUtils.TypePatternPathMatchResult, pathMatchResult);
                    await context.Insert(new List<Handler> { handler }, additionalRegistry);
                }
                else
                {
                    await context.Next();
                }
            };
        }
    }
}
