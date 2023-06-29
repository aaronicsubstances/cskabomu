using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class DefaultQuasiHttpBody : IQuasiHttpBody2
    {
        public long ContentLength { get; set; }

        public string ContentType { get; set; }

        public ICustomReader Reader { get; set; }

        public ICustomWritable<IDictionary<string, object>> Writable { get; set; }
    }
}
