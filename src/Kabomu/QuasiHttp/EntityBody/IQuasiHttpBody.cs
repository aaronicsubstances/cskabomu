using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents the body of a quasi HTTP request or response.
    /// </summary>
    public interface IQuasiHttpBody : ISelfWritable, ICustomDisposable
    {
        /// <summary>
        /// Gets the number of bytes that the instance will supply,
        /// or -1 (actually any negative value) to indicate an unknown number of bytes.
        /// </summary>
        long ContentLength { get; }

        /// <summary>
        /// Gets a reader acceptable by <see cref="IOUtils.ReadBytes"/>,
        /// for reading byte representation of the instance. Can also return null to
        /// indicate that direct reading is not supported.
        /// </summary>
        object Reader { get; }
    }
}
