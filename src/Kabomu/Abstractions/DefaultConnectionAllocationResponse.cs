using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Provides default implementation of <see cref="IConnectionAllocationResponse"/>
    /// interface.
    /// </summary>
    public class DefaultConnectionAllocationResponse : IConnectionAllocationResponse
    {
        /// <summary>
        /// Gets or sets the object that a quasi http transport instance can
        /// use to read or write data.
        /// </summary>
        public IQuasiHttpConnection Connection { get; set; }

        /// <summary>
        /// Gets or sets an optional task that would have to be completed before
        /// connection property will be fully ready to use.
        /// </summary>
        public Task ConnectTask { get; set; }
    }
}
