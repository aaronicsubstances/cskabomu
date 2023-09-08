using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    public interface IEncodedQuasiHttpEntity
    {
        byte[] Headers { get; set; }
        Stream Body { get; set; }
    }
}
