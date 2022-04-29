using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpRequestMessage
    {
        public string Path { get; set; }
        public QuasiHttpKeyValueCollection Headers { get; set; }
        public IQuasiHttpBody Body { get; set; }
    }
}
