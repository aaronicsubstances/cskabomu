using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface ICustomReader : ICustomDisposable
    {
        Task<int> ReadAsync(byte[] data, int offset, int length);
    }
}
