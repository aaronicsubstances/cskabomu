using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public interface IEncodedReadRequest
    {
        byte[] Headers { get; }
        object Body { get; }
    }
}
