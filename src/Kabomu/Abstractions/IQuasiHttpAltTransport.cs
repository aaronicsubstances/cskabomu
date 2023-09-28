using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    public interface IQuasiHttpAltTransport
    {
        Func<IQuasiHttpConnection, IQuasiHttpRequest, Task> RequestSerializer { get; }
        Func<IQuasiHttpConnection, IQuasiHttpResponse, Task> ResponseSerializer { get; }
        Func<IQuasiHttpConnection, Task<IQuasiHttpRequest>> RequestDeserializer { get; }
        Func<IQuasiHttpConnection, Task<IQuasiHttpResponse>> ResponseDeserializer { get; }
    }
}
