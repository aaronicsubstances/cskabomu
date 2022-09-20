using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.ResponseRendering
{
    /// <summary>
    /// Represents an object that can render itself to a quasi http context response
    /// without the need for an instance of <see cref="IResponseRenderer"/> interface.
    /// </summary>
    public interface IRenderable
    {
        /// <summary>
        /// Uses a quasi http context to render itself.
        /// </summary>
        /// <param name="context">the quasi http context</param>
        /// <returns>a task representing the asynchronous operation</returns>
        Task Render(IContext context);
    }
}
