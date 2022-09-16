using System.Collections.Generic;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Extension of <see cref="IHeadersWrapper"/> which provides mutable operations.
    /// </summary>
    public interface IMutableHeadersWrapper : IHeadersWrapper
    {
        /// <summary>
        /// Adds a new header name and value. If the header name exists already, its existing values are
        /// appended with the new value argument.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="value">header value</param>
        /// <returns>instance on which this method was invoked, for chaining more mutable operations</returns>
        IMutableHeadersWrapper Add(string name, string value);

        /// <summary>
        /// Adds a new header name and values. If the header name exists already, its existing values are
        /// appended with the new values argument.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="values">header value</param>
        /// <returns>instance on which this method was invoked, for chaining more mutable operations</returns>
        IMutableHeadersWrapper Add(string name, IEnumerable<string> values);

        /// <summary>
        /// Sets a new header name and value. If the header name exists already, its existing values are
        /// replaced.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="value">header value</param>
        /// <returns>instance on which this method was invoked, for chaining more mutable operations</returns>
        IMutableHeadersWrapper Set(string name, string value);

        /// <summary>
        /// Sets a new header name and values. If the header name exists already, its existing values are
        /// replaced.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="values">header values</param>
        /// <returns>instance on which this method was invoked, for chaining more mutable operations</returns>
        IMutableHeadersWrapper Set(string name, IEnumerable<string> values);

        /// <summary>
        /// Removes all header names and values.
        /// </summary>
        /// <returns>instance on which this method was invoked, for chaining more mutable operations</returns>
        IMutableHeadersWrapper Clear();

        /// <summary>
        /// Removes all header values for a given name.
        /// </summary>
        /// <param name="name">header name</param>
        /// <returns>instance on which this method was invoked, for chaining more mutable operations</returns>
        IMutableHeadersWrapper Remove(string name);
    }
}