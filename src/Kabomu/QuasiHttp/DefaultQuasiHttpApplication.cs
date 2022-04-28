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
        }

        public List<QuasiHttpMiddleware> Middlewares { get; }

        public void Use(params QuasiHttpSimpleMiddleware[] functions)
        {
            foreach (var f in functions)
            {
                QuasiHttpMiddleware wrapper = (req, reqAtts, resCb, next) =>
                {
                    f.Invoke(req, reqAtts, resCb);
                    next.Invoke(null, null);
                };
                Middlewares.Add(wrapper);
            }
        }

        public void Use(string path, params QuasiHttpSimpleMiddleware[] functions)
        {
            foreach (var f in functions)
            {
                QuasiHttpMiddleware wrapper = (req, reqAtts, resCb, next) =>
                {
                    if (req.Path == path)
                    {
                        f.Invoke(req, reqAtts, resCb);
                    }
                    next.Invoke(null, null);
                };
                Middlewares.Add(wrapper);
            }
        }

        public void UseWithNext(params QuasiHttpMiddleware[] functions)
        {
            foreach (var f in functions)
            {
                Middlewares.Add(f);
            }
        }

        public void UseWithNext(string path, params QuasiHttpMiddleware[] functions)
        {
            foreach (var f in functions)
            {
                QuasiHttpMiddleware wrapper = (req, reqAtts, resCb, next) =>
                {
                    if (req.Path != path)
                    {
                        next.Invoke(null, null);
                        return;
                    }
                    f.Invoke(req, reqAtts, resCb, next);
                };
                Middlewares.Add(f);
            }
        }

        public void ProcessPostRequest(QuasiHttpRequestMessage request, Action<Exception, QuasiHttpResponseMessage> cb)
        {
            // apply all application level middlewares and path-specific router level middlewares in order.
            // use middlewares to handle serialization with probability, and deserialization.
            if (Middlewares.Count > 0)
            {
                var requestAttributes = new Dictionary<string, object>();
                RunNextMiddleware(request, requestAttributes, cb, 0);
            }
        }

        private void RunNextMiddleware(QuasiHttpRequestMessage request,
            Dictionary<string, object> requestAttributes,
            Action<Exception, QuasiHttpResponseMessage> cb, int nextIndex)
        {
            QuasiHttpMiddlewareContinuationCallback next = (newRequest, newRequestAttributes) =>
            {
                if (nextIndex + 1 < Middlewares.Count)
                {
                    RunNextMiddleware(newRequest ?? request,
                        newRequestAttributes ?? requestAttributes,
                        cb, nextIndex + 1);
                }
            };
            var nextMiddleware = Middlewares[nextIndex];
            if (nextMiddleware != null)
            {
                nextMiddleware.Invoke(request, requestAttributes, cb, next);
            }
            else
            {
                next.Invoke(null, null);
            }
        }
    }
}
