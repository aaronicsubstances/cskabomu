using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents commonality of functions provided by TCP or IPC mechanisms
    /// at both server and client ends.
    /// </summary>
    public interface IQuasiHttpTransport
    {
        /// <summary>
        /// Transfers an entire http entity to a quasi web transport
        /// </summary>
        /// <param name="connection">connection to use for transfer</param>
        /// <param name="isResponse">indicates whether http entity is for
        /// response (with truth value), or indicates request (with false value)</param>
        /// <param name="entity">http request or response entity to transfer</param>
        /// <returns>a task representing transfer operation</returns>
        Task Write(IQuasiHttpConnection connection, bool isResponse,
            IEncodedQuasiHttpEntity entity);

        /// <summary>
        /// Retrieves an entire http entity from a quasi web transport.
        /// </summary>
        /// <param name="connection">connection to use for retrieval</param>
        /// <param name="isResponse">indicates whether http entity is for
        /// response (with truth value), or indicates request (with false value)</param>
        /// <returns>a task whose result will be an http request or response entity.</returns>
        Task<IEncodedQuasiHttpEntity> Read(IQuasiHttpConnection connection,
            bool isResponse);
    }
}
