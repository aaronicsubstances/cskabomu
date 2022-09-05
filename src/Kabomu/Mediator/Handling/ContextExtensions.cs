using Kabomu.Concurrency;
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
    public static class ContextExtensions
    {
        public static IPathMatchResult GetPathMatchResult(this IRegistry registry)
        {
            return RegistryExtensions.Get<IPathMatchResult>(registry,
                ContextUtils.RegistryKeyPathMatchResult);
        }

        public static IPathTemplateGenerator GetPathTemplateGenerator(this IRegistry registry)
        {
            return RegistryExtensions.Get<IPathTemplateGenerator>(registry,
                   ContextUtils.RegistryKeyPathTemplateGenerator);
        }

        public static IPathTemplate ParseUnboundRequestTarget(this IRegistry registry, string part1, object part2)
        {
            var pathTemplateGenerator = GetPathTemplateGenerator(registry);
            IPathTemplate pathTemplate = pathTemplateGenerator.Parse(part1, part2);
            return pathTemplate;
        }

        public static async Task<T> ParseRequest<T>(this IContext context, object parseOpts)
        {
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task<T>)> parserCode = (obj) =>
                {
                    var parser = obj as IRequestParser;
                    if (parser != null && parser.CanParse<T>(context, parseOpts))
                    {
                        var result = parser.Parse<T>(context, parseOpts);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task<T> parserTask;
                using (await context.MutexApi.Synchronize())
                {
                    bool found;
                    (found, parserTask) = RegistryExtensions.TryGetFirst(context,
                        ContextUtils.RegistryKeyRequestParser, parserCode);
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

        public static async Task RenderResponse(this IContext context, object body)
        {
            try
            {
                if (body is IRenderable renderable)
                {
                    await renderable.Render(context);
                    return;
                }
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> renderingCode = (obj) =>
                {
                    var renderer = obj as IResponseRenderer;
                    if (renderer != null && renderer.CanRender(context, body))
                    {
                        var result = renderer.Render(context, body);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task renderTask;
                using (await context.MutexApi.Synchronize())
                {
                    bool found;
                    (found, renderTask) = RegistryExtensions.TryGetFirst(context,
                        ContextUtils.RegistryKeyResponseRenderer, renderingCode);
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

        public static async Task HandleUnexpectedEnd(this IContext context)
        {
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> unexpectedEndCode = (obj) =>
                {
                    var handler = obj as IUnexpectedEndHandler;
                    if (handler != null)
                    {
                        var result = handler.HandleUnexpectedEnd(context);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task unexpectedEndTask;
                using (await context.MutexApi.Synchronize())
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
                if (!(e is HandlerException))
                {
                    await context.HandleError(new HandlerException(null, e));
                }
                else
                {
                    await context.HandleError(e);
                }
            }
        }

        private static Task HandleUnexpectedEndLastResort(IContext context)
        {
            return context.Response.SetStatusCode(404).TrySend();
        }

        public static async Task HandleError(this IContext context, Exception error)
        {
            try
            {
                // tolerate prescence of nulls.
                Func<object, (bool, Task)> errorHandlingCode = (obj) =>
                {
                    var handler = obj as IServerErrorHandler;
                    if (handler != null)
                    {
                        var result = handler.HandleError(context, error);
                        return (true, result);
                    }
                    return (false, null);
                };
                Task resultTask;
                using (await context.MutexApi.Synchronize())
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

        private static async Task HandleErrorLastResort(IContext context, Exception original,
            Exception errorHandlerException)
        {
            Task sendTask;
            using (await context.MutexApi.Synchronize())
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

                sendTask = context.Response.SetStatusCode(500)
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
    }
}
