using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface ICustomWriter : ICustomDisposable
    {
        Task WriteBytes(byte[] data, int offset, int length);
    }
}
