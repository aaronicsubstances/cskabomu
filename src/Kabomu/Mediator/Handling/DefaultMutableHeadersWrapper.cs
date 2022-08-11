using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultMutableHeadersWrapper : IMutableHeaders
    {
        private readonly Func<IDictionary<string, IList<string>>> _rawHeadersGetter;
        private readonly Action<IDictionary<string, IList<string>>> _rawHeadersSetter;

        public DefaultMutableHeadersWrapper(Func<IDictionary<string, IList<string>>> rawHeadersGetter,
            Action<IDictionary<string, IList<string>>> rawHeadersSetter)
        {
            _rawHeadersGetter = rawHeadersGetter;
            _rawHeadersSetter = rawHeadersSetter;
        }

        private IDictionary<string, IList<string>> GetRawHeaders(bool createIfNecessary)
        {
            var rawHeaders = _rawHeadersGetter.Invoke();
            if (rawHeaders == null && createIfNecessary)
            {
                rawHeaders = new Dictionary<string, IList<string>>();
                _rawHeadersSetter.Invoke(rawHeaders);
            }
            return rawHeaders;
        }

        public string Get(string name)
        {
            var rawHeaders = GetRawHeaders(false);
            if (rawHeaders != null)
            {
                IList<string> values = null;
                if (rawHeaders.ContainsKey(name))
                {
                    values = rawHeaders[name];
                }
                if (values != null && values.Count > 0)
                {
                    return values[0];
                }
            }
            return null;
        }

        public IMutableHeaders Add(string name, string value)
        {
            var rawHeaders = GetRawHeaders(true);
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

        public IMutableHeaders Clear()
        {
            var rawHeaders = GetRawHeaders(false);
            rawHeaders?.Clear();
            return this;
        }

        public IMutableHeaders Remove(string name)
        {
            var rawHeaders = GetRawHeaders(false);
            rawHeaders?.Remove(name);
            return this;
        }

        public IMutableHeaders Set(string name, string value)
        {
            var rawHeaders = GetRawHeaders(true);
            rawHeaders.Remove(name);
            rawHeaders.Add(name, new List<string> { value });
            return this;
        }

        public IMutableHeaders Set(string name, IEnumerable<string> values)
        {
            var rawHeaders = GetRawHeaders(true);
            rawHeaders.Remove(name);
            rawHeaders.Add(name, new List<string>(values));
            return this;
        }
    }
}
