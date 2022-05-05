using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpKeyValueCollection
    {
        public Dictionary<string, List<string>> Content { get; } = new Dictionary<string, List<string>>();
    }
}
