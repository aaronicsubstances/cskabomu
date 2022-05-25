using Kabomu.Common;
using Kabomu.QuasiWsgi;
using System;
using System.Collections.Generic;

namespace Kabomu.Internals
{
    internal class DefaultQuasiHttpContext : IQuasiHttpContext
    {
        public IQuasiHttpRequestMessage Request { get; set; }
        public Dictionary<string, object> RequestAttributes { get; set; }
        public IQuasiHttpResponseMessage Response { get; set; }
        public bool ResponseMarkedAsSent { get; set; }
        public Exception Error { get; set; }
    }
}