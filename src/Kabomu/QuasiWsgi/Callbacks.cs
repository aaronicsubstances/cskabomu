using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiWsgi
{
    public delegate void QuasiHttpSimpleMiddleware(
        IQuasiHttpContext context,
        Action<Exception, object> responseCb);

    public delegate void QuasiHttpMiddleware(
        IQuasiHttpContext context,
        Action<Exception, object> responseCb, 
        QuasiHttpMiddlewareContinuationCallback next);

    public delegate void QuasiHttpMiddlewareContinuationCallback(
        Action<Exception, object> responseCb,
        Exception error);
}