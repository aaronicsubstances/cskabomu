using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class CustomWritableBackedBody : AbstractQuasiHttpBody
    {
        private readonly ICustomWritable _writable;

        public CustomWritableBackedBody(ICustomWritable writable)
        {
            if (writable == null)
            {
                throw new ArgumentNullException(nameof(writable));
            }
            _writable = writable;
        }

        public override Task CustomDispose() => _writable.CustomDispose();

        public override Task WriteBytesTo(ICustomWriter writer) =>
            _writable.WriteBytesTo(writer);
    }
}
