using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Represents a destination of bytes to which bytes can be written.
    /// </summary>
    public interface ICustomWriter : ICustomDisposable
    {
        /// <summary>
        /// Writes bytes to the instance.
        /// </summary>
        /// <param name="data">the source buffer of the bytes to be
        /// fetched for writing to this instance</param>
        /// <param name="offset">the starting position in buffer for
        /// fetching the bytes to be written</param>
        /// <param name="length">the number of bytes to write to instance</param>
        /// <returns>a task representing end of the asynchronous operation</returns>
        Task WriteBytes(byte[] data, int offset, int length);
    }
}
