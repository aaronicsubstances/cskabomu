using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpPdu
    {
        public const byte Version01 = 1;

        public const byte PduTypeRequest = 1;
        public const byte PduTypeResponse = 2;
        public const byte PduTypeRequestFin = 3;
        public const byte PduTypeResponseFin = 4;
        public const byte PduTypeRequestChunkGet = 5;
        public const byte PduTypeRequestChunkRet = 6;
        public const byte PduTypeResponseChunkGet = 7;
        public const byte PduTypeResponseChunkRet = 8;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
        public int RequestId { get; set; }
        public string Path { get; set; }
        public bool StatusIndicatesSuccess { get; set; }
        public bool StatusIndicatesClientError { get; set; }
        public string StatusMessage { get; set; }
        public int ContentLength { get; set; }
        public string ContentType { get; set; }
        public QuasiHttpKeyValueCollection Headers { get; set; }
        public byte[] EmbeddedBody { get; set; }
        public int EmbeddedBodyOffset { get; set; }

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
