using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpRequestMessage : IQuasiHttpRequestMessage
    {
        public string Path { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
        public IQuasiHttpBody Body { get; set; }
    }
}
