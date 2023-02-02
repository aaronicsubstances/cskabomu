using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal interface IRequestProcessorInternal
    {
        Task AbortWithError(Exception error);
    }
}
