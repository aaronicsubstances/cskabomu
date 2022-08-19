using Kabomu.Mediator.Path;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    public static class ContextExtensions
    {
        public static Handler MountPath(this IContext context, string pathSpec, Handler handler)
        {
            if (pathSpec == null)
            {
                throw new ArgumentNullException(nameof(pathSpec));
            }
            var pathTemplateGenerator = context.PathTemplateGenerator;
            IPathTemplate pathTemplate = pathTemplateGenerator.Parse(pathSpec);
            return HandlerUtils.MountPath(pathTemplate, handler);
        }
    }
}
