using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    public class QuasiHttpPdu
    {
        public const byte Version01 = 1;
        public const byte PduTypeRequest = 1;
        public const byte PduTypeResponse = 2;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
        public int RequestId { get; set; }
        public QuasiHttpRequestMessage Request { get; set; }
        public QuasiHttpResponseMessage Response { get; set; }
        public string Verb { get; set; }
        public byte[] EmbeddedBody { get; set; }
        public int EmbeddedBodyOffset { get; set; }
        public int EmbeddedBodyLength { get; set; }

        public static QuasiHttpPdu Deserialize(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
