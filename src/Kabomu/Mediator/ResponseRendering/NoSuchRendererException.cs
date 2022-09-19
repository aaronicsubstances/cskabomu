using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.ResponseRendering
{
    /// <summary>
    /// Thrown by <see cref="Handling.ContextExtensions.RenderResponse"/> method when an 
    /// appropriate response renderer is not found in instance of <see cref="Handling.IContext"/> class.
    /// </summary>
    public class NoSuchRendererException : MediatorQuasiWebException
    {
        /// <summary>
        /// Creates a new instance with a default error message.
        /// </summary>
        public NoSuchRendererException() :
            base("No appropriate response renderer found")
        {
        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public NoSuchRendererException(string message) : base(message)
        {

        }
    }
}
