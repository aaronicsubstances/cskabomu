using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultContextRequestInternal : IContextRequest
    {
        private readonly DefaultMutableRegistry _registry;

        public DefaultContextRequestInternal(IQuasiHttpRequest rawRequest)
        {
            RawRequest = rawRequest ?? throw new ArgumentNullException(nameof(rawRequest));
            Headers = new DefaultMutableHeadersWrapper(_ => rawRequest.Headers);
            _registry = new DefaultMutableRegistry();
        }

        public IQuasiHttpRequest RawRequest { get; }

        public string Method => RawRequest.Method;

        public string Target => RawRequest.Target;

        public IHeadersWrapper Headers { get; }

        public IQuasiHttpBody Body => RawRequest.Body;

        public IMutableRegistry Add(object key, object value)
        {
            return _registry.Add(key, value);
        }

        public IMutableRegistry AddGenerator(object key, Func<object> valueGenerator)
        {
            return _registry.AddGenerator(key, valueGenerator);
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
