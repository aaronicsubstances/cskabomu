using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public interface IRequestParser
    {
        Task<T> Parse<T>(IContext context, IQuasiHttpBody body, object parseOpts);
    }
}
