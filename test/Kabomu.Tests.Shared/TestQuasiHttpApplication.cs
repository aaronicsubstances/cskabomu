using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class TestQuasiHttpApplication : IQuasiHttpApplication
    {
        private readonly Action<IQuasiHttpRequest, Action<Exception, IQuasiHttpResponse>> _processingCb;

        public TestQuasiHttpApplication(Action<IQuasiHttpRequest, Action<Exception, IQuasiHttpResponse>> cb)
        {
            _processingCb = cb;
        }

        public void ProcessRequest(IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
        {
            _processingCb.Invoke(request, cb);
        }
    }
}
