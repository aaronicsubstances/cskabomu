using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class ProxyQuasiHttpResponse : IQuasiHttpResponse
    {
        private readonly IQuasiHttpResponse _delegate;
        private readonly IQuasiHttpBody _body;

        public ProxyQuasiHttpResponse(IQuasiHttpResponse d)
        {
            _delegate = d;
            _body = d.Body == null ? null : new ProxyBody(d.Body);
        }

        public int StatusCode => _delegate.StatusCode;

        public IDictionary<string, IList<string>> Headers => _delegate.Headers;

        public IQuasiHttpBody Body => _body;

        public string HttpStatusMessage => _delegate.HttpStatusMessage;

        public string HttpVersion => _delegate.HttpVersion;

        public IDictionary<string, object> Environment => _delegate.Environment;

        public Task Close()
        {
            return _delegate.Close();
        }
    }
}
