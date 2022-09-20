using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.RequestParsing
{
    /// <summary>
    /// Represents functions that can deserialize supported objects from quasi http context
    /// requests.
    /// </summary>
    public interface IRequestParser
    {
        /// <summary>
        /// Determines which objects that instances of this interface support for deserialization
        /// from quasi http context requests.
        /// </summary>
        /// <typeparam name="T">type of object to test</typeparam>
        /// <param name="context">quasi http context</param>
        /// <param name="parseOpts">any optional or required options</param>
        /// <returns>true if this instance can deserialize with the given arguments;
        /// false it cannot deserialize.</returns>
        bool CanParse<T>(IContext context, object parseOpts);

        /// <summary>
        /// Deserializes an object from a quasi http context request. Should fail if
        /// object type or deserialization options fails the test done by the 
        /// <see cref="CanParse{T}(IContext, object)"/> method.
        /// </summary>
        /// <typeparam name="T">type of object to deserialize</typeparam>
        /// <param name="context">quasi http context</param>
        /// <param name="parseOpts">any optional or required options</param>
        /// <returns>a task whose result will be an object parsed from the quasi http context</returns>
        Task<T> Parse<T>(IContext context, object parseOpts);
    }
}
