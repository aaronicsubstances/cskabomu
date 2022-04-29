using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public delegate void QuasiHttpSimpleMiddleware(
        QuasiHttpRequestMessage request,
        Dictionary<string, object> requestAttributes,
        Action<Exception, object> responseCb);

    public delegate void QuasiHttpMiddleware(
        QuasiHttpContext context,
        Dictionary<string, object> requestAttributes, 
        Action<Exception, object> responseCb, 
        QuasiHttpMiddlewareContinuationCallback next);

    public delegate void QuasiHttpMiddlewareContinuationCallback(
        Dictionary<string, object> requestAttributes,
        Action<Exception, object> responseCb,
        Exception error);

    public delegate void QuasiHttpBodyCallback(Exception error, byte[] data, int offset, int length);
}