using System.Collections.Generic;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Represents request and response headers used with the Kabomu.Mediator quasi web framework.
    /// </summary>
    public interface IHeadersWrapper
    {
        /// <summary>
        /// Gets the first of header values for a given name. 
        /// </summary>
        /// <param name="name">header name</param>
        /// <returns>first header value or null if no values exist for name</returns>
        string Get(string name);

        /// <summary>
        /// Gets all header values for given name.
        /// </summary>
        /// <param name="name">header name</param>
        /// <returns>all header values or empty list if header name is not found.</returns>
        IEnumerable<string> GetAll(string name);

        /// <summary>
        /// Gets all header names.
        /// </summary>
        ICollection<string> GetNames();
    }
}