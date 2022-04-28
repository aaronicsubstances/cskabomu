﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpResponseMessage
    {
        public bool StatusIndicatesSuccess { get; set; }
        public bool StatusIndicatesClientError { get; set; }
        public string StatusMessage { get; set; }
        public QuasiHttpKeyValueCollection Headers { get; set; }
        public object Body { get; set; }
    }
}
