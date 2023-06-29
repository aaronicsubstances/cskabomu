using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface ICustomWritable<T> : ICustomDisposable
    {
        Task WriteToAsync(ICustomWriter writer, T context);
    }
}
