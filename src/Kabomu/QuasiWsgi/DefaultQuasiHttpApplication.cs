using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiWsgi
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
                QuasiHttpMiddleware wrapper = (context, resCb, next) =>
                {
                    if (path == null || context.Request.Path == path)
                    {
                        if (f is QuasiHttpSimpleMiddleware simple)
                        {
                            if (context.Response == null)
                            {
                                simple.Invoke(context, resCb);
                            }
                            else
                            {
                                next.Invoke(null, null);
                            }
                        }
                        else
                        {
                            ((QuasiHttpMiddleware)f).Invoke(context, resCb, next);
                        }
                    }
                    else
                    {
                        next.Invoke(null, null);
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
        
        public void ProcessRequest(IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> responseCb)
        {
            // apply all application level middlewares, path-specific router level middlewares,
            // and error middlewares in order.
            // also use middlewares to handle serialization and deserialization.
            var context = new DefaultQuasiHttpContext
            {
                Request = request,
                RequestAttributes = new Dictionary<string, object>()
            };
            Action<Exception, object> responseCbWrapper = (e, res) =>
            {
                if (res == null)
                {
                    res = new DefaultQuasiHttpResponse
                    {
                        StatusIndicatesSuccess = true
                    };
                }
                context.Response = (IQuasiHttpResponse)res;
                responseCb.Invoke(e, context.Response);
                context.ResponseMarkedAsSent = true;
            };
            RunNextMiddleware(context, responseCbWrapper, 0);
        }

        private void RunNextMiddleware(DefaultQuasiHttpContext context,
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
            QuasiHttpMiddlewareContinuationCallback next = (newResponseCb, error) =>
            {
                int nextIndex = index + 1;
                if (error != null)
                {
                    if (context.Error == null)
                    {
                        nextIndex = 0;
                    }
                    context.Error = error;
                }
                RunNextMiddleware(
                    context,
                    newResponseCb ?? responseCb,
                    nextIndex);
            };
            if (nextMiddleware != null)
            {
                try
                {
                    nextMiddleware.Invoke(context, responseCb, next);
                }
                catch (Exception ex)
                {
                    if (context.Error == null)
                    {
                        next.Invoke(null, ex);
                    }
                    else
                    {
                        // swallow error somehow since error middleware is responsible for this error.
                    }
                }
            }
            else
            {
                next.Invoke(null, null);
            }
        }
    }
}
