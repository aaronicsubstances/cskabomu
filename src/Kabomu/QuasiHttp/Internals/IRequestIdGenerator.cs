using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal interface IRequestIdGenerator
    {
        int NextId();
    }
}
