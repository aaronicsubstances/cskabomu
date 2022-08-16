using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.ResponseRendering
{
    public interface IResponseRenderer
    {
        bool CanRender(IContext context, object obj);
        Task Render(IContext context, object obj);
    }
}
