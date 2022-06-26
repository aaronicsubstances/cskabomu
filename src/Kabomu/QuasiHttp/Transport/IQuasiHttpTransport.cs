using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IQuasiHttpTransport
    {
        Task WriteBytes(object connection, byte[] data, int offset, int length);
        Task<int> ReadBytes(object connection, byte[] data, int offset, int length);
        Task ReleaseConnection(object connection);
    }
}
