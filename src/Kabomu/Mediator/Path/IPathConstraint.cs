using Kabomu.Mediator.Handling;
using System.Collections.Generic;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// Represents contraints that can be applied during path matching and interpolation.
    /// </summary>
    public interface IPathConstraint
    {
        /// <summary>
        /// Applies a constraint function to a captured path segment.
        /// </summary>
        /// <param name="context">quasi http context</param>
        /// <param name="pathTemplate">path template instance</param>
        /// <param name="values">captured path segments</param>
        /// <param name="valueKey">key of captured path segment in values</param>
        /// <param name="constraintArgs">any constraint args</param>
        /// <param name="direction">equals <see cref="ContextUtils.PathConstraintMatchDirectionMatch"/> if
        /// constraint is being applied during path match time; equals
        /// <see cref="ContextUtils.PathConstraintMatchDirectionFormat"/> if
        /// constraint is being applied during path interpolation time</param>
        /// <returns>true if and only if constraint passes</returns>
        bool ApplyCheck(IContext context, IPathTemplate pathTemplate, IDictionary<string, string> values, 
            string valueKey, string[] constraintArgs, int direction);
    }
}