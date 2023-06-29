using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface ICustomWriter : ICustomDisposable
    {
        Task WriteAsync(byte[] data, int offset, int length);

        Task FlushAsync();
    }
}
