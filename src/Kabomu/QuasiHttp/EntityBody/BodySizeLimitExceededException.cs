using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class BodySizeLimitExceededException : Exception
    {
        public BodySizeLimitExceededException(string message) : base(message)
        {
        }
    }
}
