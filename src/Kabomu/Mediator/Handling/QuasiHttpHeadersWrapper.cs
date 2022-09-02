using System;
using System.Collections.Generic;

namespace Kabomu.Mediator.Handling
{
    public class QuasiHttpHeadersWrapper : IMutableHeaders
    {
        private readonly Func<IDictionary<string, IList<string>>> _getter;
        private readonly Action<IDictionary<string, IList<string>>> _setter;

        public QuasiHttpHeadersWrapper(Func<IDictionary<string, IList<string>>> getter,
            Action<IDictionary<string, IList<string>>> setter)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter; // null acceptable for readonly headerwrappers.
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

        public IMutableHeaders Clear()
        {
            _getter.Invoke()?.Clear();
            return this;
        }

        public IMutableHeaders Remove(string name)
        {
            _getter.Invoke()?.Remove(name);
            return this;
        }

        public IMutableHeaders Add(string name, string value)
        {
            var rawHeaders = GetOrCreateRawHeaders();
            IList<string> values;
            if (rawHeaders.ContainsKey(name))
            {
                values = rawHeaders[name];
            }
            else
            {
                values = new List<string>();
                rawHeaders.Add(name, values);
            }
            values.Add(value);
            return this;
        }

        public IMutableHeaders Set(string name, string value)
        {
            var rawHeaders = GetOrCreateRawHeaders();
            rawHeaders.Remove(name);
            rawHeaders.Add(name, new List<string> { value });
            return this;
        }

        public IMutableHeaders Set(string name, IEnumerable<string> values)
        {
            var rawHeaders = GetOrCreateRawHeaders();
            rawHeaders.Remove(name);
            rawHeaders.Add(name, new List<string>(values));
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
