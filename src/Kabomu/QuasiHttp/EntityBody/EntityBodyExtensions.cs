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
        /// Returns a custom reader for a quasi http body instance.
        /// If the body already implements <see cref="ICustomReader"/> then
        /// the instance is returned as is. Else <see cref="IOUtils.CoalesceAsReader"/>
        /// is used to get a custom reader.
        /// </summary>
        /// <param name="body">the quasi http body</param>
        /// <returns>custom reader which can be used to read bytes from the body</returns>
        public static ICustomReader AsReader(this IQuasiHttpBody body)
        {
            return IOUtils.CoalesceAsReader(null, body);
        }
    }
}
