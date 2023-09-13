using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Contains connection and other connection-related information created by
    /// <see cref="IQuasiHttpClientTransport"/> instances
    /// in response to a connection allocation request.
    /// </summary>
    public interface IConnectionAllocationResponse
    {
        /// <summary>
        /// Gets the object that a quasi http transport instance can
        /// use to read or write data.
        /// </summary>
        IQuasiHttpConnection Connection { get; }

        /// <summary>
        /// Gets an optional task that would have to be completed before
        /// connection property will be fully ready to use.
        /// </summary>
        Task ConnectTask { get; }
    }
}
