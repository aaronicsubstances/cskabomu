using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.ResponseRendering
{
    /// <summary>
    /// Represents functions that can serialize supported objects to quasi http context
    /// responses.
    /// </summary>
    public interface IResponseRenderer
    {
        /// <summary>
        /// Determines which objects that instances of this interface support for rendering or serialization
        /// to quasi http context responses.
        /// </summary>
        /// <param name="context">quasi http context</param>
        /// <param name="obj">object to test</param>
        /// <returns>true if this instance can render/serialize the <paramref name="obj"/> argument; false
        /// if the object argument cannot be serialized.</returns>
        bool CanRender(IContext context, object obj);

        /// <summary>
        /// Renders/serializes an object to a quasi http context response. Should fail if
        /// object fails the test done by the <see cref="CanRender(IContext, object)"/> method.
        /// </summary>
        /// <param name="context">quasi http context</param>
        /// <param name="obj">object to render.</param>
        /// <returns>a task asynchronous operation</returns>
        Task Render(IContext context, object obj);
    }
}
