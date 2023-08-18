using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// Used by <see cref="IPathTemplateGenerator"/> instances to represent templates for matching
    /// request paths and targets, and which can be used to interpolate paths. It is comparable to a compiled
    /// regular expression object.
    /// </summary>
    public interface IPathTemplate
    {
        /// <summary>
        /// Tries to match a request target with an instance of this interface, and
        /// if match suceeds, returns an object with details of the match.
        /// </summary>
        /// <param name="context">quasi http context which may be of use to an implementation</param>
        /// <param name="requestTarget">request target to match</param>
        /// <returns>match details or null if requestTarget argument matches this instance or not
        /// respectively</returns>
        IPathMatchResult Match(IContext context, string requestTarget);

        /// <summary>
        /// Interpolates a path with path segments filled in by user supplied values, and
        /// in accordance with a particular instance of this interface. Must succeed or 
        /// fail.
        /// </summary>
        /// <remarks>
        /// If an implementation can interpolate in more than 1 way, then it must employ a
        /// deterministic way to determine the interpolation to return, and communicate that
        /// to clients.
        /// </remarks>
        /// <param name="context">quasi http context which may be of use to an implementation</param>
        /// <param name="pathValues">values with which to fill in path segments</param>
        /// <param name="options">any options object which can direct interpolation</param>
        /// <returns>interpolated path</returns>
        string Interpolate(IContext context, IDictionary<string, object> pathValues,
            object options);

        /// <summary>
        /// Computes all path interpolations possible with a given set of values for path segments, in
        /// accordance with a particular instance of this interface.
        /// </summary>
        /// <param name="context">quasi http context which may be of use to an implementation</param>
        /// <param name="pathValues">values with which to fill in path segments</param>
        /// <param name="options">any options object which can direct interpolation</param>
        /// <returns>all possible interpolated paths, or empty list if no interpolation is possible</returns>
        IList<string> InterpolateAll(IContext context, IDictionary<string, object> pathValues,
            object options);
    }
}
