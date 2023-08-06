using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class DummyQuasiHttpBody : IQuasiHttpBody
    {
        public long ContentLength { get; set; }

        public ICustomReader Reader() => throw new NotImplementedException();

        public Task CustomDispose()
        {
            throw new NotImplementedException();
        }

        public Task WriteBytesTo(ICustomWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
