using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public delegate void QuasiHttpSimpleMiddleware(QuasiHttpRequestMessage request,
        Dictionary<string, object> requestAttributes,
        Action<Exception, QuasiHttpResponseMessage> responseCb);

    public delegate void QuasiHttpMiddleware(QuasiHttpRequestMessage request,
        Dictionary<string, object> requestAttributes, 
        Action<Exception, QuasiHttpResponseMessage> responseCb, 
        QuasiHttpMiddlewareContinuationCallback next);

    public delegate void QuasiHttpMiddlewareContinuationCallback(QuasiHttpRequestMessage request,
        Dictionary<string, object> requestAttributes);

    public delegate void QuasiHttpBodyCallback(Exception error, byte[] data, int offset, int length);
}