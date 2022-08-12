using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultContextRequest : IRequest
    {
        private readonly DefaultMutableRegistry _registry;

        public DefaultContextRequest(IQuasiHttpRequest rawRequest)
        {
            RawRequest = rawRequest ?? throw new ArgumentNullException(nameof(rawRequest));
            Headers = new DefaultHeadersWrapper(rawRequest);
            _registry = new DefaultMutableRegistry();
        }

        public IQuasiHttpRequest RawRequest { get; }

        public string Path => RawRequest.Path;

        public IHeaders Headers { get; }

        public IQuasiHttpBody Body => RawRequest.Body;

        public IMutableRegistry Add(object key, object value)
        {
            return _registry.Add(key, value);

        }
        public IMutableRegistry AddValueSource(object key, IRegistryValueSource valueSource)
        {
            return _registry.AddValueSource(key, valueSource);
        }

        public object Get(object key)
        {
            return _registry.Get(key);
        }

        public IEnumerable<object> GetAll(object key)
        {
            return _registry.GetAll(key);
        }

        public IMutableRegistry Remove(object key)
        {
            return _registry.Remove(key);
        }

        public (bool, object) TryGet(object key)
        {
            return _registry.TryGet(key);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return _registry.TryGetFirst(key, transformFunction);
        }

        class DefaultHeadersWrapper : IHeaders
        {
            private readonly IQuasiHttpRequest _parent;

            public DefaultHeadersWrapper(IQuasiHttpRequest parent)
            {
                _parent = parent;
            }

            public string Get(string name)
            {
                var rawHeaders = _parent.Headers;
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
        }
    }
}
