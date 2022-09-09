using System;
using System.Collections.Generic;
using System.Linq;

namespace Kabomu.Mediator.Handling
{
    public class DefaultMutableHeadersWrapper : IMutableHeadersWrapper
    {
        private readonly Func<IDictionary<string, IList<string>>> _getter;
        private readonly Action<IDictionary<string, IList<string>>> _setter;
        private readonly IDictionary<string, IList<string>> _extensibleListReferences;

        public DefaultMutableHeadersWrapper(Func<IDictionary<string, IList<string>>> getter,
            Action<IDictionary<string, IList<string>>> setter)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter; // null acceptable for readonly header wrappers.
            _extensibleListReferences = new Dictionary<string, IList<string>>();
        }

        public string Get(string name)
        {
            var rawHeaders = _getter.Invoke();
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

        public IEnumerable<string> GetAll(string name)
        {
            var rawHeaders = _getter.Invoke();
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

        public ICollection<string> GetNames()
        {
            var rawHeaders = _getter.Invoke();
            if (rawHeaders != null)
            {
                return rawHeaders.Keys;
            }
            return Enumerable.Empty<string>().ToList();
        }

        public IMutableHeadersWrapper Clear()
        {
            _getter.Invoke()?.Clear();
            return this;
        }

        public IMutableHeadersWrapper Remove(string name)
        {
            _getter.Invoke()?.Remove(name);
            return this;
        }

        public IMutableHeadersWrapper Add(string name, string value)
        {
            return Add(name, Enumerable.Repeat(value, 1));
        }

        public IMutableHeadersWrapper Add(string name, IEnumerable<string> values)
        {
            var rawHeaders = GetOrCreateRawHeaders();
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

        public IMutableHeadersWrapper Set(string name, string value)
        {
            var rawHeaders = GetOrCreateRawHeaders();
            rawHeaders.Remove(name);
            // intentionally not creating a list directly so as to
            // test logic of Add() method when faced with unmodifiable list.
            rawHeaders.Add(name, Enumerable.Repeat(value, 1).ToList());
            return this;
        }

        public IMutableHeadersWrapper Set(string name, IEnumerable<string> values)
        {
            var rawHeaders = GetOrCreateRawHeaders();
            rawHeaders.Remove(name);
            rawHeaders.Add(name, values.ToList());
            return this;
        }

        private IDictionary<string, IList<string>> GetOrCreateRawHeaders()
        {
            var rawHeaders = _getter.Invoke();
            if (rawHeaders == null)
            {
                if (_setter == null)
                {
                    throw new InvalidOperationException("mutation operation requires non-null setter " +
                        "arg to be supplied during construction");
                }
                rawHeaders = new Dictionary<string, IList<string>>();
                _setter.Invoke(rawHeaders);
            }
            return rawHeaders;
        }
    }
}
