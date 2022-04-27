using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpResponseMessage
    {
        public bool StatusIndicatesSuccess { get; set; }
        public bool StatusIndicatesClientError { get; set; }
        public string StatusMessage { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public object CustomHeaders { get; set; }
        public object Body { get; set; }
    }
}
