using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// HTTP processing pipeline which emulates ExpressJS.
    /// </summary>
    public class DefaultQuasiHttpApplication : IQuasiHttpApplication
    {
        public DefaultQuasiHttpApplication()
        {
            Middlewares = new List<QuasiHttpMiddleware>();
            ErrorMiddlewares = new List<QuasiHttpMiddleware>();
        }

        public List<QuasiHttpMiddleware> Middlewares { get; }
        public List<QuasiHttpMiddleware> ErrorMiddlewares { get; }

        public void Use(string path, params object[] functions)
        {
            foreach (var f in functions)
            {
                if (f is QuasiHttpSimpleMiddleware || f is QuasiHttpMiddleware)
                {
                }
                else
                {
                    throw new ArgumentException("unknown middleware delegate type: " + f);
                }
                QuasiHttpMiddleware wrapper = (context, reqAtts, resCb, next) =>
                {
                    if (path == null || context.Request.Path == path)
                    {
                        if (f is QuasiHttpSimpleMiddleware simple)
                        {
                            if (!context.ResponseSent)
                            {
                                simple.Invoke(context.Request, reqAtts, resCb);
                            }
                            else
                            {
                                next.Invoke(null, null, null);
                            }
                        }
                        else
                        {
                            ((QuasiHttpMiddleware)f).Invoke(context, reqAtts, resCb, next);
                        }
                    }
                    else
                    {
                        next.Invoke(null, null, null);
                    }
                };
                Middlewares.Add(wrapper);
            }
        }

        public void UseForError(params QuasiHttpMiddleware[] functions)
        {
            foreach (var f in functions)
            {
                ErrorMiddlewares.Add(f);
            }
        }
        
        public void ProcessRequest(QuasiHttpRequestMessage request, Action<Exception, QuasiHttpResponseMessage> responseCb)
        {
            // apply all application level middlewares, path-specific router level middlewares,
            // and error middlewares in order.
            // also use middlewares to handle serialization and deserialization.
            var context = new QuasiHttpContext(request, null, false);
            var requestAttributes = new Dictionary<string, object>();
            Action<Exception, object> responseCbWrapper = (e, o) =>
            {
                var res = (QuasiHttpResponseMessage)o;
                responseCb.Invoke(e, res);
                context.MarkResponseAsSent();
            };
            RunNextMiddleware(context, requestAttributes, responseCbWrapper, 0);
        }

        private void RunNextMiddleware(QuasiHttpContext context,
            Dictionary<string, object> requestAttributes,
            Action<Exception, object> responseCb,
            int index)
        {
            QuasiHttpMiddleware nextMiddleware;
            if (context.Error == null)
            {
                if (index >= Middlewares.Count)
                {
                    return;
                }
                nextMiddleware = Middlewares[index];
            }
            else
            {
                if (index >= ErrorMiddlewares.Count)
                {
                    return;
                }
                nextMiddleware = ErrorMiddlewares[index];
            }
            QuasiHttpMiddlewareContinuationCallback next = (newRequestAttributes, newResponseCb, error) =>
            {
                int nextIndex = index + 1;
                if (context.Error == null && error != null)
                {
                    nextIndex = 0;
                }
                QuasiHttpContext newContext = context;
                if (error != context.Error)
                {
                    newContext = new QuasiHttpContext(
                        context.Request,
                        error ?? context.Error,
                        context.ResponseSent);
                }
                Action<Exception, object> newResponseCbWrapper = null;
                if (newResponseCb != null)
                {
                    newResponseCbWrapper = (e, o) =>
                    {
                        newResponseCb.Invoke(e, o);
                        context.MarkResponseAsSent();
                    };
                }
                RunNextMiddleware(
                    newContext,
                    newRequestAttributes ?? requestAttributes,
                    newResponseCbWrapper ?? responseCb,
                    nextIndex);
            };
            if (nextMiddleware != null)
            {
                try
                {
                    nextMiddleware.Invoke(context, requestAttributes, responseCb, next);
                }
                catch (Exception ex)
                {
                    if (context.Error == null)
                    {
                        next.Invoke(null, null, ex);
                    }
                    else
                    {
                        // swallow error somehow since error middleware is responsible for this error.
                    }
                }
            }
            else
            {
                next.Invoke(null, null, null);
            }
        }
    }
}
