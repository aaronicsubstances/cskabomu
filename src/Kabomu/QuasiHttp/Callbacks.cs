using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public delegate void QuasiHttpMiddlewareCallback(QuasiHttpRequestMessage request, Action<Exception, object> cb);

    public delegate void QuasiHttpBodyCallback(Exception error, byte[] data, int offset, int length);
}
