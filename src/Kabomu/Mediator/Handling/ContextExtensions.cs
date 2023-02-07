﻿using Kabomu.Concurrency;
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
            lock (context.Mutex)
            {
                bool found;
                (found, parserTask) = RegistryExtensions.TryGetFirst(context,
                    ContextUtils.RegistryKeyRequestParser, parserCode);
                if (!found)
                {
                    throw ContextUtils.CreateNoSuchParserExceptionForKey(
                        ContextUtils.RegistryKeyRequestParser);
                }
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
                lock (context.Mutex)
                {
                    bool found;
                    (found, renderTask) = RegistryExtensions.TryGetFirst(context,
                        ContextUtils.RegistryKeyResponseRenderer, renderingCode);
                    if (!found)
                    {
                        throw ContextUtils.CreateNoSuchRendererExceptionForKey(
                            ContextUtils.RegistryKeyResponseRenderer);
                    }
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
                lock (context.Mutex)
                {
                    bool found;
                    (found, unexpectedEndTask) = RegistryExtensions.TryGetFirst(context,
                        ContextUtils.RegistryKeyUnexpectedEndHandler, unexpectedEndCode);
                    if (!found)
                    {
                        unexpectedEndTask = HandleUnexpectedEndLastResort(context);
                    }
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
                lock (context.Mutex)
                {
                    bool found;
                    (found, resultTask) = RegistryExtensions.TryGetFirst(context,
                        ContextUtils.RegistryKeyServerErrorHandler, errorHandlingCode);
                    if (!found)
                    {
                        resultTask = HandleErrorLastResort(context, error, null);
                    }
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
            lock (context.Mutex)
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
                context.Response.TrySend(() =>
                {
                    context.Response.SetServerErrorStatusCode()
                        .SetBody(new StringBody(msg) { ContentType = "text/plain" });
                });
            }
            return Task.CompletedTask;
        }

        internal static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                if (!string.IsNullOrEmpty(exception.Message))
                {
                    stringBuilder.AppendLine(exception.Message);
                }
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    stringBuilder.AppendLine(exception.StackTrace);
                }

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
