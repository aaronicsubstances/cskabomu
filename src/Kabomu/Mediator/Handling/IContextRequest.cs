using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Represents an association of a quasi http request with a mutable dictionary of request attributes.
    /// </summary>
    public interface IContextRequest : IMutableRegistry
    {
        /// <summary>
        /// Gets the underlying quasi http request.
        /// </summary>
        IQuasiHttpRequest RawRequest { get; }

        /// <summary>
        /// Gets the method of the <see cref="RawRequest"/> property.
        /// </summary>
        string Method { get; }

        /// <summary>
        /// Gets the target of the <see cref="RawRequest"/> property.
        /// </summary>
        string Target { get; }

        /// <summary>
        /// Returns an instance of <see cref="IHeadersWrapper"/> class that
        /// provides convenient access to the headers in the <see cref="RawRequest"/> property.
        /// </summary>
        IHeadersWrapper Headers { get; }

        /// <summary>
        /// Gets the body of the <see cref="RawRequest"/> property.
        /// </summary>
        IQuasiHttpBody Body { get; }
    }
}
