using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultHeadersWrapper : IHeaders
    {
        private readonly IDictionary<string, IList<string>> _rawHeaders;

        public DefaultHeadersWrapper(IDictionary<string, IList<string>> rawHeaders)
        {
            _rawHeaders = rawHeaders;
        }

        public string Get(string name)
        {
            IList<string> values = null;
            if (_rawHeaders.ContainsKey(name))
            {
                values = _rawHeaders[name];
            }
            if (values != null && values.Count > 0)
            {
                return values[0];
            }
            return null;
        }
    }
}
