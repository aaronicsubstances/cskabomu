using Kabomu.Mediator.Path;
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
    /// <summary>
    /// Provides extension methods applicable to all implementations of <see cref="IContext"/> interface.
    /// </summary>
    public static class ContextExtensions
    {
        /// <summary>
        /// Creates a handler which will insert a variable number of handlers conditional on whether a path match is found between
        /// the path template generated from the most current instance of <see cref="IPathTemplateGenerator"/> in a given
        /// registry, and the unbound request path of the most current instance of the <see cref="IPathMatchResult"/> class
        /// in the provided <see cref="IContext"/> argument. By default the
        /// path template will be generated from CSV specs as expected by the <see cref="DefaultPathTemplateGenerator"/> class.
        /// </summary>
        /// <remarks>
        /// If a match is found, the inserted handlers will see a new instance of <see cref="IPathMatchResult"/>
        /// corresponding to the match as the most current instance in the context. Else 
        /// the created handler will just call the next handler in the context.
        /// </remarks>
        /// <param name="context">the context with the <see cref="IPathTemplateGenerator"/> instance to
        /// be used.</param>
        /// <param name="path">the string specification of the path template acceptable to the path template generator
        /// in the given registry. defaults to CSV specs as expected by the <see cref="DefaultPathTemplateGenerator"/> class.</param>
        /// <param name="handlers">the handlers which will be inserted if the generated path template matches
        /// the most current unbound request path at the time the created handler is invoked</param>
        /// <returns>new handler which represents deferred execution of handlers conditional on
        /// unbound request path</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="context"/> argument is null.</exception>
        /// <exception cref="NotInRegistryException">The <paramref name="context"/> argument does not
        /// contain the key equal to <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/>.</exception>
        public static Handler HandlePath(this IRegistry context, string path, params Handler[] handlers)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            var pathTemplate = GeneratePathTemplate(context, path, null);
            return HandlerUtils.ByPath(pathTemplate, handlers);
        }

        /// <summary>
        /// Gets the most current instance of <see cref="IPathMatchResult"/> class from a given registry using
        /// the key of <see cref="ContextUtils.RegistryKeyPathMatchResult"/>. Key must be found or else exception will be thrown.
        /// </summary>
        /// <param name="context">the context with the
        /// <see cref="IPathMatchResult"/> instance</param>
        /// <returns>most current path match result</returns>
        /// <exception cref="NotInRegistryException">If key of
        /// <see cref="ContextUtils.RegistryKeyPathMatchResult"/>
        /// was not found.</exception>
        public static IPathMatchResult GetPathMatchResult(this IRegistry context)
        {
            return RegistryExtensions.Get<IPathMatchResult>(context,
                ContextUtils.RegistryKeyPathMatchResult);
        }

        /// <summary>
        /// Gets the most current instance of <see cref="IPathTemplateGenerator"/> class from a given registry using
        /// the key of <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/>.
        /// Key must be found or else exception will be thrown.
        /// </summary>
        /// <param name="context">the context with the
        /// <see cref="IPathTemplateGenerator"/> instance</param>
        /// <returns>most current path template generator</returns>
        /// <exception cref="NotInRegistryException">If key of
        /// <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/>
        /// was not found.</exception>
        public static IPathTemplateGenerator GetPathTemplateGenerator(this IRegistry context)
        {
            return RegistryExtensions.Get<IPathTemplateGenerator>(context,
                ContextUtils.RegistryKeyPathTemplateGenerator);
        }

        /// <summary>
        /// Generates a path template from a string specification and compatible options, using the most current instance of 
        /// <see cref="IPathTemplateGenerator"/> stored in a given registry under the key of
        /// <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/>. Key must be found or else exception will be thrown.
        /// </summary>
        /// <param name="context">the context with the
        /// <see cref="IPathTemplateGenerator"/> instance to use</param>
        /// <param name="spec">string specification</param>
        /// <param name="options">options accompanying string spec</param>
        /// <returns>path template generated from spec with most current path template generator</returns>
        /// <exception cref="NotInRegistryException">If key of
        /// <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/>
        /// was not found.</exception>
        public static IPathTemplate GeneratePathTemplate(this IRegistry context, string spec, object options)
        {
            var pathTemplateGenerator = GetPathTemplateGenerator(context);
            IPathTemplate pathTemplate = pathTemplateGenerator.Parse(spec, options);
            return pathTemplate;
        }

        /// <summary>
        /// Searches inside a context's registry under the key of <see cref="ContextUtils.RegistryKeyRequestParser"/>,
        /// for the first request parser (instance of <see cref="IRequestParser"/> class) which indicates that 
        /// it can parse the context's request (by returning true from its <see cref="IRequestParser.CanParse"/> method).
        /// The <see cref="IRequestParser.Parse"/> method of the instance is then called upon
        /// to deserialize the context's request body to a given object type.
        /// </summary>
        /// <typeparam name="T">Target object type of deserialized request body</typeparam>
        /// <param name="context">the context with the request and request parsers</param>
        /// <param name="parseOpts">any deserialization options. it is up to request parsers to
        /// determine whether such options are required, and of what type, and to return false from
        /// <see cref="IRequestParser.CanParse"/> if a required option or expected type is missing.</param>
        /// <returns>task whose result will be an object of given type deserialized from request of context argument</returns>
        /// <exception cref="NoSuchParserException">No request parser was found, or all
        /// the found request parsers returned false from their <see cref="IRequestParser.CanParse"/> method.</exception>
        public static async Task<T> ParseRequest<T>(this IContext context, object parseOpts)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            // tolerate prescence of nulls.
            Func<object, (bool, Task<T>)> parserCode = (obj) =>
            {
                var parser = (IRequestParser)obj;
                if (parser != null && parser.CanParse<T>(context, parseOpts))
                {
                    var result = parser.Parse<T>(context, parseOpts);
                    return (true, result);
                }
                return (false, null);
            };
            Task<T> parserTask;
            bool found;
            (found, parserTask) = RegistryExtensions.TryGetFirst(context,
                ContextUtils.RegistryKeyRequestParser, parserCode);
            if (!found)
            {
                throw ContextUtils.CreateNoSuchParserExceptionForKey(
                    ContextUtils.RegistryKeyRequestParser);
            }
            return await parserTask;
        }

        /// <summary>
        /// Searches inside a context's registry under the key of <see cref="ContextUtils.RegistryKeyResponseRenderer"/>,
        /// for the first response renderer (instance of <see cref="IResponseRenderer"/> class) which indicates that 
        /// it can commit the context's response with a given object (by returning true from 
        /// its <see cref="IResponseRenderer.CanRender"/> method). The <see cref="IResponseRenderer.CanRender"/> method 
        /// of the instance is then called upon to commit the context's response with the given object.
        /// </summary>
        /// <remarks>
        /// If the given object implements <see cref="IRenderable"/> interface, then its <see cref="IRenderable.Render"/>
        /// is called and the whole search for response renderers is skipped.
        /// <para></para>
        /// Else a response renderer will be involved, which is expected to
        /// convert the given object into an instances of <see cref="IQuasiHttpBody"/>, and call one of the
        /// IContextResponse.Send* method on the response of the context.
        /// </remarks>
        /// <param name="context">the context with the response and response renderers</param>
        /// <param name="body">the object to serialize</param>
        /// <returns>task representing the asynchronous operation</returns>
        /// <exception cref="NoSuchRendererException">The given object does not implement <see cref="IResponseRenderer"/> interface,
        /// and no response renderer was found, or all the found response renderers returned false from
        /// their <see cref="IResponseRenderer.CanRender"/> method</exception>
        public static async Task RenderResponse(this IContext context, object body)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (body is IRenderable renderable)
            {
                await renderable.Render(context);
            }
            else
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> renderingCode = (obj) =>
                {
                    var renderer = (IResponseRenderer)obj;
                    if (renderer != null && renderer.CanRender(context, body))
                    {
                        var result = renderer.Render(context, body);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task renderTask;
                bool found;
                (found, renderTask) = RegistryExtensions.TryGetFirst(context,
                    ContextUtils.RegistryKeyResponseRenderer, renderingCode);
                if (!found)
                {
                    throw ContextUtils.CreateNoSuchRendererExceptionForKey(
                        ContextUtils.RegistryKeyResponseRenderer);
                }
                await renderTask;
            }
        }

        /// <summary>
        /// Uses the first non-null instance of <see cref="IUnexpectedEndHandler"/> class found
        /// in a context's registry under the key of <see cref="ContextUtils.RegistryKeyUnexpectedEndHandler"/>,
        /// to deal with situation where a context does not have any handler to call next. Can also be used by
        /// handlers to deal with unreachable code situations.
        /// </summary>
        /// <remarks>
        /// By default if no instance of <see cref="IUnexpectedEndHandler"/> class is found, response is
        /// committed with an empty body and a 404 status code.
        /// </remarks>
        /// <param name="context">the context with instances of the <see cref="IUnexpectedEndHandler"/> class</param>
        /// <returns>task representing the asynchronous operation</returns>
        public static async Task HandleUnexpectedEnd(this IContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> unexpectedEndCode = (obj) =>
                {
                    var handler = (IUnexpectedEndHandler)obj;
                    if (handler != null)
                    {
                        var result = handler.HandleUnexpectedEnd(context);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task unexpectedEndTask;
                bool found;
                (found, unexpectedEndTask) = RegistryExtensions.TryGetFirst(context,
                    ContextUtils.RegistryKeyUnexpectedEndHandler, unexpectedEndCode);
                if (!found)
                {
                    unexpectedEndTask = HandleUnexpectedEndLastResort(context);
                }
                await unexpectedEndTask;
            }
            catch (Exception e)
            {
                await context.HandleError(e);
            }
        }

        private static Task HandleUnexpectedEndLastResort(IContext context)
        {
            context.Response.TrySend(() =>
            {
                context.Response.SetStatusCode(404);
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Uses the first error handler, ie instance of <see cref="IServerErrorHandler"/> class found
        /// in a context's registry under the key of <see cref="ContextUtils.RegistryKeyServerErrorHandler"/>,
        /// to deal with errors.
        /// </summary>
        /// <remarks>
        /// By default if no instance of <see cref="IServerErrorHandler"/> class is found, response is
        /// committed with a body made up of serialized error object and a 500 status code. The error
        /// object is serialized by combining messages and stack traces from it and its nested child error objects. 
        /// </remarks>
        /// <param name="context">the context with instances of the <see cref="IServerErrorHandler"/> class</param>
        /// <param name="error">error object</param>
        /// <returns>a task representing the asynchronous operation</returns>
        public static async Task HandleError(this IContext context, Exception error)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> errorHandlingCode = (obj) =>
                {
                    var handler = (IServerErrorHandler)obj;
                    if (handler != null)
                    {
                        var result = handler.HandleError(context, error);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task resultTask;
                bool found;
                (found, resultTask) = RegistryExtensions.TryGetFirst(context,
                    ContextUtils.RegistryKeyServerErrorHandler, errorHandlingCode);
                if (!found)
                {
                    resultTask = HandleErrorLastResort(context, error, null);
                }
                await resultTask;
            }
            catch (Exception e)
            {
                try
                {
                    await HandleErrorLastResort(context, error, e);
                }
                catch (Exception) { }
            }
        }

        private static Task HandleErrorLastResort(IContext context, Exception original,
            Exception errorHandlerException)
        {
            string msg;
            if (errorHandlerException != null)
            {
                msg = "Exception thrown by error handler while handling exception\n" +
                    "Original exception: " + original + "\n" +
                    "Error handler exception: " + errorHandlerException;
            }
            else
            {
                msg = original.ToString();
            }
            context.Response.TrySend(() =>
            {
                context.Response.Headers.Set("content-type", "text/plain");
                context.Response.SetServerErrorStatusCode()
                    .SetBody(new StringBody(msg));
            });
            return Task.CompletedTask;
        }
    }
}
