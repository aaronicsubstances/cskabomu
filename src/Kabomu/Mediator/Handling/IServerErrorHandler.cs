using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public interface IServerErrorHandler
    {
        Task HandleError​(IContext context, Exception error);
    }
}
