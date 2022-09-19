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
    /// <summary>
    /// Provides helper methods for creating <see cref="Handler"/> instances.
    /// </summary>
    public static class HandlerUtils
    {
        /// <summary>
        /// Creates a handler which will act as a chain in the handler stack represented by an instance of
        /// <see cref="IContext"/> class, by inserting a list of handlers when invoked. If the list has only 1 item,
        /// then that item is returned.
        /// </summary>
        /// <param name="handlers">list of handlers which will constitute a chain</param>
        /// <returns>representative chain handler</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="handlers"/> argument is null</exception>
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

        /// <summary>
        /// Creates a handler which will act as a chain in the handler stack represented by an instance of
        /// <see cref="IContext"/> class, by inserting a variable number of handler arguments when invoked. If only 1 argument is
        /// given, then that argument is returned.
        /// </summary>
        /// <param name="handlers">handler arguments which will constitute a chain</param>
        /// <returns>representative chain handler</returns>
        public static Handler Chain(params Handler[] handlers)
        {
            return Chain((IList<Handler>)handlers);
        }

        /// <summary>
        /// Creates a handler which when invoked, calls the next handler in an instance of
        /// <see cref="IContext"/> such that the handler will see an extension to the
        /// registry of the context.
        /// </summary>
        /// <param name="registry">registry with which to extend existing view of quasi context registry</param>
        /// <returns>new handler which represents a deferred call to <see cref="IContext.Next(IRegistry)"/></returns>
        public static Handler Register(IRegistry registry)
        {
            return context =>
            {
                context.Next(registry);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Creates a handler which when invoked, inserts a variable number of handler arguments and
        /// extends the registry presented by an instance of <see cref="IContext"/> with a given registry.
        /// </summary>
        /// <param name="registry">registry with which to extend existing view of quasi context registry</param>
        /// <param name="handlers">handler arguments to insert.</param>
        /// <returns>new handler which represents a deferred call to <see cref="IContext.Insert(IList{Handler}, IRegistry)"/></returns>
        public static Handler Register(IRegistry registry, params Handler[] handlers)
        {
            return context =>
            {
                context.Insert(handlers, registry);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether a path match is found between
        /// the path template generated from the most current instance of <see cref="IPathTemplateGenerator"/> in a given
        /// registry, and the unbound request path of the most current instance of the <see cref="IPathMatchResult"/> class
        /// in the <see cref="IContext"/> present at the time of execution of the created handler.
        /// </summary>
        /// <remarks>
        /// If a match is found, the inserted handlers will see a new instance of <see cref="IPathMatchResult"/>
        /// corresponding to the match as the most current instance in the context. Else 
        /// the created handler will just call the next handler in the context.
        /// </remarks>
        /// <param name="registry">the registry containing the instance of the <see cref="IPathTemplateGenerator"/> to
        /// be used. Must be non null and contain the path template generator or else an error will be thrown.</param>
        /// <param name="spec">the string specification of the path template acceptable to the path template generator
        /// in the given registry.</param>
        /// <param name="options">the options accompanying the path template to be generated</param>
        /// <param name="handlers">the handlers which will be inserted if the generated path template matches
        /// the most current unbound request path at the time the created handler is invoked</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on
        /// unbound request path</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="registry"/> argument is null.</exception>
        /// <exception cref="NotInRegistryException">The <paramref name="registry"/> argument does not
        /// contain the key equal to <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/>.</exception>
        public static Handler Path(IRegistry registry, string spec, object options, params Handler[] handlers)
        {
            var pathTemplate = ContextUtils.ParseUnboundRequestTarget(registry, spec, options);
            return Path(pathTemplate, handlers);
        }

        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether a path match is found between
        /// a given path template, and the unbound request path of the most current instance of the
        /// <see cref="IPathMatchResult"/> class in the <see cref="IContext"/> present at the time of
        /// execution of the created handler.
        /// </summary>
        /// <remarks>
        /// If a match is found, the inserted handlers will see a new instance of <see cref="IPathMatchResult"/>
        /// corresponding to the match as the most current instance in the context. Else 
        /// the created handler will just call the next handler in the context.
        /// </remarks>
        /// <param name="pathTemplate">path template to use</param>
        /// <param name="handlers">the handlers which will be inserted if the given path template matches
        /// the most current unbound request path at the time the created handler is invoked</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on
        /// unbound request path</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="pathTemplate"/> argument is null.</exception>
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

        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether the
        /// quasi http request method is "GET".
        /// </summary>
        /// <remarks>
        /// If the quasi http request method is not "GET", the created handler will just call the next handler in the context.
        /// </remarks>
        /// <param name="handlers">handlers to insert conditionally on request method of "GET"</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on request method equals "GET"</returns>
        public static Handler ByGet(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodGet, handlers);
        }

        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether the
        /// quasi http request method is "POST".
        /// </summary>
        /// <remarks>
        /// If the quasi http request method is not "POST", the created handler will just call the next handler in the context.
        /// </remarks>
        /// <param name="handlers">handlers to insert conditionally on request method of "POST"</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on request method equals "POST"</returns>
        public static Handler ByPost(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodPost, handlers);
        }

        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether the
        /// quasi http request method is "PUT".
        /// </summary>
        /// <remarks>
        /// If the quasi http request method is not "PUT", the created handler will just call the next handler in the context.
        /// </remarks>
        /// <param name="handlers">handlers to insert conditionally on request method of "PUT"</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on request method equals "PUT"</returns>
        public static Handler ByPut(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodPut, handlers);
        }

        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether the
        /// quasi http request method is "DELETE".
        /// </summary>
        /// <remarks>
        /// If the quasi http request method is not "DELETE", the created handler will just call the next handler in the context.
        /// </remarks>
        /// <param name="handlers">handlers to insert conditionally on request method of "DELETE"</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on request method equals "DELETE"</returns>
        public static Handler ByDelete(params Handler[] handlers)
        {
            return ByMethod(DefaultQuasiHttpRequest.MethodDelete, handlers);
        }

        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether the
        /// quasi http request method equals a given value.
        /// </summary>
        /// <param name="method">request method to match</param>
        /// <param name="handlers">handlers to insert conditionally on request method</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on request method</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="method"/> argument is null</exception>
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
