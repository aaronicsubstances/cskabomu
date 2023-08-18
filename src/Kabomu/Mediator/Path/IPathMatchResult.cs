using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// Used by <see cref="IPathTemplate"/> instances to capture results of successful matches against
    /// request paths and targets.
    /// </summary>
    public interface IPathMatchResult
    {
        /// <summary>
        /// Gets path segments captured in a successful match.
        /// </summary>
        IDictionary<string, object> PathValues { get; }

        /// <summary>
        /// Gets the prefix of a request path or target which was matched.
        /// </summary>
        /// <remarks>
        /// If a match is always attempted on all of a request target, then this property will always 
        /// equal the matched request target. In any case, clients of this class must ensure that
        /// a matched request target always equals this property appended with <see cref="UnboundRequestTarget"/>.
        /// </remarks>
        string BoundPath { get; }

        /// <summary>
        /// Gets the suffix of a request path or target which was not matched.
        /// </summary>
        /// <remarks>
        /// If a match is always attempted on all of a request target, then this property will always 
        /// be an empty string. In any case, clients of this class must ensure that
        /// a matched request target always equals this property prepended with <see cref="BoundPath"/>.
        /// </remarks>
        string UnboundRequestTarget { get; }
    }
}
