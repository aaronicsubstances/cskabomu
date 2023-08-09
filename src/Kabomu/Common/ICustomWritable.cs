using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Represents an instance that can transfer bytes by itself
    /// to a destination of bytes.
    /// </summary>
    public interface ICustomWritable
    {
        /// <summary>
        /// Transfers some byte representation of the instance to
        /// a writer.
        /// </summary>
        /// <param name="writer">the writer which will receive the
        /// byte representation of this instance</param>
        /// <returns>task representing end of write operation</returns>
        Task WriteBytesTo(object writer);
    }
}
