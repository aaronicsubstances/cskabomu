using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public abstract class AbstractQuasiHttpBody : IQuasiHttpBody
    {
        public long ContentLength { get; set; } = -1;

        public string ContentType { get; set; }

        public virtual ICustomReader Reader => this as ICustomReader;

        public virtual Task WriteBytesTo(ICustomWriter writer)
        {
            var reader = Reader;
            if (reader == null)
            {
                throw new MissingDependencyException(
                    "ICustomReader not implemented");
            }
            return IOUtils.CopyBytes(reader, writer);
        }

        public abstract Task CustomDispose();
    }
}
