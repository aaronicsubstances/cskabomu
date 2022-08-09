using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public interface IRenderer
    {
        Type RenderableType { get; }
        Task Render(IContext context, object obj);
    }
}
