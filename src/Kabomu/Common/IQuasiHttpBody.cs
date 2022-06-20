using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IQuasiHttpBody
    {
        long ContentLength { get; }
        string ContentType { get; }
        Task<int> ReadBytes(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead);
        Task EndRead(IEventLoopApi eventLoop, Exception e);
    }
}
