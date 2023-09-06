using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class DefaultEncodedReadRequest : IEncodedReadRequest
    {
        public byte[] Headers { get; set; }

        public object Body { get; set; }
    }
}
