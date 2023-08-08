using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Represents a source of bytes from which bytes can be read.
    /// </summary>
    public interface ICustomReader
    {
        /// <summary>
        /// Reads bytes from the instance.
        /// </summary>
        /// <param name="data">the destination buffer where bytes read will be stored</param>
        /// <param name="offset">starting position in buffer for storing bytes read</param>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>a task whose result will be the number of bytes actually read</returns>
        Task<int> ReadBytes(byte[] data, int offset, int length);
    }
}
