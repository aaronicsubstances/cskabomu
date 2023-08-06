using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kabomu.QuasiHttp.EntityBody
{
    public static class EntityBodyExtensions
    {
        /// <summary>
        /// Calls upon <see cref="IOUtils.CoalesceAsReader"/> to get a custom reader
        /// for the body.
        /// </summary>
        /// <param name="body">the quasi http body</param>
        /// <returns>custom reader which can be used to read bytes from the body</returns>
        public static ICustomReader AsReader(this IQuasiHttpBody body)
        {
            return IOUtils.CoalesceAsReader(body?.Reader(), body);
        }
    }
}
