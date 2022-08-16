using Kabomu.Mediator.Handling;
using System.Collections.Generic;

namespace Kabomu.Mediator.Path
{
    public interface IPathConstraint
    {
        bool Match(IContext context, IPathTemplate pathTemplate, IDictionary<string, string> values, 
            string valueKey, string[] constraintArgs, int direction);
    }
}