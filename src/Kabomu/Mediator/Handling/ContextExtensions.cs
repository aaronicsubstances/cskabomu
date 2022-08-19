using Kabomu.Mediator.Path;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    public static class ContextExtensions
    {
        public static Handler MountPath(this IContext context, string part1, object part2, Handler handler)
        {
            var pathTemplateGenerator = context.PathTemplateGenerator;
            IPathTemplate pathTemplate = pathTemplateGenerator.Parse(part1, part2);
            return HandlerUtils.MountPath(pathTemplate, handler);
        }
    }
}
