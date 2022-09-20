using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Provides a default implementation of a manager of quasi http request or response headers.
    /// </summary>
    public class DefaultMutableHeadersWrapper : IMutableHeadersWrapper
    {
        private readonly Func<bool, IDictionary<string, IList<string>>> _dictCb;
        private readonly IDictionary<string, IList<string>> _extensibleListReferences;

        /// <summary>
        /// Creates new instance, using a procedure to get and create an underlying dictionary of raw headers.
        /// </summary>
        /// <param name="dictCb">procedure whose argument indicates "create if necesary"</param>
        /// <exception cref="ArgumentNullException">The <paramref name="dictCb"/> argument is null.</exception>
        public DefaultMutableHeadersWrapper(Func<bool, IDictionary<string, IList<string>>> dictCb)
        {
            _dictCb = dictCb ?? throw new ArgumentNullException(nameof(dictCb));
            _extensibleListReferences = new Dictionary<string, IList<string>>();
        }

        /// <summary>
        /// Gets the first of header values for a given name. 
        /// </summary>
        /// <param name="name">header name</param>
        /// <returns>first header value or null if no values exist for name</returns>
        public string Get(string name)
        {
            var rawHeaders = GetOrCreateRawHeaders(false);
            IList<string> values = null;
            if (rawHeaders != null && rawHeaders.ContainsKey(name))
            {
                values = rawHeaders[name];
            }
            if (values != null && values.Count > 0)
            {
                return values[0];
            }
            return null;
        }

        /// <summary>
        /// Gets all header values for given name.
        /// </summary>
        /// <param name="name">header name</param>
        /// <returns>all header values or empty list if header name is not found.</returns>
        public IEnumerable<string> GetAll(string name)
        {
            var rawHeaders = GetOrCreateRawHeaders(false);
            if (rawHeaders != null && rawHeaders.ContainsKey(name))
            {
                var values = rawHeaders[name];
                if (values != null)
                {
                    return values;
                }
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets all header names or an empty list if no underlying dictionary exists.
        /// </summary>
        public ICollection<string> GetNames()
        {
            var rawHeaders = GetOrCreateRawHeaders(false);
            if (rawHeaders != null)
            {
                return rawHeaders.Keys;
            }
            return Enumerable.Empty<string>().ToList();
        }

        /// <summary>
        /// Removes all header names and values.
        /// </summary>
        /// <returns>instance on which this method was invoked, for chaining more operations</returns>
        public IMutableHeadersWrapper Clear()
        {
            var rawHeaders = GetOrCreateRawHeaders(false);
            rawHeaders?.Clear();
            return this;
        }

        /// <summary>
        /// Removes all header values for a given name.
        /// </summary>
        /// <param name="name">header name</param>
        /// <returns>instance on which this method was invoked, for chaining more operations</returns>
        public IMutableHeadersWrapper Remove(string name)
        {
            var rawHeaders = GetOrCreateRawHeaders(false);
            rawHeaders?.Remove(name);
            return this;
        }

        /// <summary>
        /// Adds a new header name and value. If the header name exists already, its existing values are
        /// appended with the new value argument.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="value">header value</param>
        /// <returns>instance on which this method was invoked, for chaining more operations</returns>
        public IMutableHeadersWrapper Add(string name, string value)
        {
            return Add(name, Enumerable.Repeat(value, 1));
        }

        /// <summary>
        /// Adds a new header name and values. If the header name exists already, its existing values are
        /// appended with the new values argument.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="values">header value</param>
        /// <returns>instance on which this method was invoked, for chaining more operations</returns>
        public IMutableHeadersWrapper Add(string name, IEnumerable<string> values)
        {
            var rawHeaders = GetOrCreateRawHeaders(true);
            // Ensure we don't run into a situation where
            // we have to add to an ungrowable list supplied from
            // outside this class, such as a fixed-length native array.
            UpdateExtensibleListReferences(rawHeaders);
            IList<string> existingValues;
            if (rawHeaders.ContainsKey(name))
            {
                existingValues = rawHeaders[name];
                if (!_extensibleListReferences.ContainsKey(name))
                {
                    existingValues = new List<string>(existingValues);
                    rawHeaders[name] = existingValues;
                    _extensibleListReferences.Add(name, existingValues);
                }
            }
            else
            {
                existingValues = new List<string>();
                rawHeaders.Add(name, existingValues);
                _extensibleListReferences.Add(name, existingValues);
            }
            foreach (var v in values)
            {
                existingValues.Add(v);
            }
            return this;
        }

        private void UpdateExtensibleListReferences(IDictionary<string, IList<string>> rawHeaders)
        {
            // empty cache unless all of its keys are in raw headers with
            // corresponding matching references.
            bool clear = false;
            foreach (var e in _extensibleListReferences)
            {
                if (!rawHeaders.ContainsKey(e.Key) || e.Value != rawHeaders[e.Key])
                {
                    clear = true;
                    break;
                }
            }
            if (clear)
            {
                _extensibleListReferences.Clear();
            }
        }

        /// <summary>
        /// Sets a new header name and value. If the header name exists already, its existing values are
        /// replaced.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="value">header value</param>
        /// <returns>instance on which this method was invoked, for chaining more operations</returns>
        public IMutableHeadersWrapper Set(string name, string value)
        {
            var rawHeaders = GetOrCreateRawHeaders(true);
            rawHeaders.Remove(name);
            // intentionally not creating a list directly so as to
            // test logic of Add() method when faced with unmodifiable list.
            rawHeaders.Add(name, Enumerable.Repeat(value, 1).ToList());
            return this;
        }

        /// <summary>
        /// Sets a new header name and values. If the header name exists already, its existing values are
        /// replaced.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="values">header values</param>
        /// <returns>instance on which this method was invoked, for chaining more operations</returns>
        public IMutableHeadersWrapper Set(string name, IEnumerable<string> values)
        {
            var rawHeaders = GetOrCreateRawHeaders(true);
            rawHeaders.Remove(name);
            rawHeaders.Add(name, values.ToList());
            return this;
        }

        private IDictionary<string, IList<string>> GetOrCreateRawHeaders(bool createIfNecessary)
        {
            var rawHeaders = _dictCb.Invoke(createIfNecessary);
            if (createIfNecessary && rawHeaders == null)
            {
                throw new InvalidOperationException("mutable operation not supported due to null " +
                    "received from dictionary callback");
            }
            return rawHeaders;
        }
    }
}
