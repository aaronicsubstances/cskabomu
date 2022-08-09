using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public interface IRenderable
    {
        Task Render(IContext context);
    }
}
