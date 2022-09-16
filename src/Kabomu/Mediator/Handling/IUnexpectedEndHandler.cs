using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Represents an object that can be called upon when a context runs out of handlers during
    /// quasi http processing. That is, a handler asked its context object to execute the next
    /// handler in line, when that handler was the last handler in the context.
    /// </summary>
    public interface IUnexpectedEndHandler
    {
        /// <summary>
        /// Implements any custom logic to deal with a context running out of handlers, such as responding with
        /// a 404 status code.
        /// </summary>
        /// <param name="context">quasi http context</param>
        /// <returns>a task representing the asynchronous operation</returns>
        Task HandleUnexpectedEnd​(IContext context);
    }
}
