using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Abstractions;

namespace Kabomu.Impl
{
    public class DefaultEncodedQuasiHttpEntity : IEncodedQuasiHttpEntity
    {
        public byte[] Headers { get; set; }

        public Stream Body { get; set; }
    }
}
