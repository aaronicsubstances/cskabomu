using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Represents an object that can deal with errors that occur during the processing of quasi http requests.
    /// </summary>
    public interface IServerErrorHandler
    {
        /// <summary>
        /// Processes the given exception that occurred processing the given context. 
        /// </summary>
        /// <remarks>
        /// Implementations should strive to avoid throwing exceptions.
        /// </remarks>
        /// <param name="context">quasi http context.</param>
        /// <param name="error">the error which occured.</param>
        /// <returns>a task representing the asynchronous operation.</returns>
        Task HandleError​(IContext context, Exception error);
    }
}
