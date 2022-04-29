using System;
using System.Threading;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpContext
    {
        private int _responseSent;

        public QuasiHttpContext(QuasiHttpRequestMessage request, Exception error, bool responseSent)
        {
            Request = request;
            Error = error;
            _responseSent = responseSent ? 1 : 0;
        }

        public QuasiHttpRequestMessage Request { get; }
        public Exception Error { get; }

        public bool ResponseSent => _responseSent == 1;

        public void MarkResponseAsSent()
        {
            Interlocked.CompareExchange(ref _responseSent, 1, 0);
        }
    }
}