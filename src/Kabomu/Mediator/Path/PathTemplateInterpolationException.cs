using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public class PathTemplateInterpolationException : MediatorQuasiWebException
    {
        public PathTemplateInterpolationException()
        {
        }

        public PathTemplateInterpolationException(string message) : base(message)
        {
        }

        public PathTemplateInterpolationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
