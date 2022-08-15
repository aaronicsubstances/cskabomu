using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public interface IUnexpectedEndHandler
    {
        Task HandleUnexpectedEnd​(IContext context);
    }
}
