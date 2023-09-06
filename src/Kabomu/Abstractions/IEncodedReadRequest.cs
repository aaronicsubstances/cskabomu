using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    public interface IEncodedReadRequest
    {
        byte[] Headers { get; set; }
        object Body { get; set; }
    }
}
