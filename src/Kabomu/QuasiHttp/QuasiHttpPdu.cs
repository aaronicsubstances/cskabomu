using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpPdu
    {
        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
        public int RequestId { get; set; }
        public int EmbeddedQuasiHttpBodyLength { get; set; }
        public QuasiHttpRequestMessage WrappedRequest { get; set; }
        public QuasiHttpResponseMessage WrappedResponse { get; set; }
        public string Verb { get; set; }
        public string BodyLocation { get; set; }
        public byte[] EmbeddedBody { get; set; }
        public int EmbeddedBodyOffset { get; set; }
    }
}
