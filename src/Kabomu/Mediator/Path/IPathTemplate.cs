using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public interface IPathTemplate
    {
        IPathMatchResult Match(IContext context, string requestTarget);
        string Interpolate(IContext context, IDictionary<string, string> pathValues,
            IPathTemplateFormatOptions options);
        IList<string> InterpolateAll(IContext context, IDictionary<string, string> pathValues,
            IPathTemplateFormatOptions options);
    }
}
