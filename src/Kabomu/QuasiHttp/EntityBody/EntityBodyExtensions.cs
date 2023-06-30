using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kabomu.QuasiHttp.EntityBody
{
    public static class EntityBodyExtensions
    {
        public static ICustomReader AsReader(this IQuasiHttpBody body)
        {
            return IOUtils.CoalesceAsReader(body.Reader, body.Writable);
        }

        public static ICustomWritable AsWritable(this IQuasiHttpBody body)
        {
            return IOUtils.CoaleasceAsWritable(body.Writable,
                body.Reader, body.ContentLength, 0);
        }
    }
}
