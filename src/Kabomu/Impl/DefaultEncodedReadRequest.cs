using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Abstractions;

namespace Kabomu.Impl
{
    public class DefaultEncodedReadRequest : IEncodedReadRequest
    {
        public byte[] Headers { get; set; }

        public object Body { get; set; }
    }
}
