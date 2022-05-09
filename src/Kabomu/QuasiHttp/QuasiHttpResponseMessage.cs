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
        public Dictionary<string, List<string>> Headers { get; set; }
        public IQuasiHttpBody Body { get; set; }
    }
}
