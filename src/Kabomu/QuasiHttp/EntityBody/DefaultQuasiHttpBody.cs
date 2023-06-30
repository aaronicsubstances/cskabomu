using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class DefaultQuasiHttpBody : IQuasiHttpBody
    {
        public long ContentLength { get; set; }

        public string ContentType { get; set; }

        public ICustomReader Reader { get; set; }

        public ICustomWritable Writable { get; set; }

        public async Task CustomDispose()
        {
            var reader = Reader;
            if (reader != null)
            {
                await reader.CustomDispose();
            }
            var writable = Writable;
            if (writable != null)
            {
                await Writable.CustomDispose();
            }
        }
    }
}
