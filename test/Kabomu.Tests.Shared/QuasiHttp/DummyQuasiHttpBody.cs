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

        public object Reader => throw new NotImplementedException();

        public Task Release()
        {
            throw new NotImplementedException();
        }

        public Task WriteBytesTo(object writer)
        {
            throw new NotImplementedException();
        }
    }
}
