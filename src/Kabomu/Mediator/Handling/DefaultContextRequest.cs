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
            Headers = new DefaultHeadersWrapper(rawRequest.Headers ?? new Dictionary<string, IList<string>>());
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

        public IMutableRegistry AddLazy(object key, Func<object> valueGenerator)
        {
            return _registry.AddLazy(key, valueGenerator);
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
    }
}
