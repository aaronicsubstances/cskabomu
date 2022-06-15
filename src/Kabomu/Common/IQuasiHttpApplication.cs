using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IQuasiHttpApplication
    {
        Task<IQuasiHttpResponse> ProcessRequestAsync(IQuasiHttpRequest request);
    }
}
