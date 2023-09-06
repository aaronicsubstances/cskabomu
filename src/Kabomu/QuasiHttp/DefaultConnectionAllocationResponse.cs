using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IConnectionAllocationResponse"/> interface
    /// in which properties of the interface are mutable.
    /// </summary>
    public class DefaultConnectionAllocationResponse : IConnectionAllocationResponse
    {
        /// <summary>
        /// Gets or sets a connection object created by a quasi http transport.
        /// </summary>
        public object Connection { get; set; }

        /// <summary>
        /// Gets or sets environment variables associated with a connection received from a
        /// quasi http transport.
        /// </summary>
        public IDictionary<string, object> Environment { get; set; }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; set; }
    }
}
