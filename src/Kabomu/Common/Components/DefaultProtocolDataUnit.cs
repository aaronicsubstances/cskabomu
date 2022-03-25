using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class DefaultProtocolDataUnit
    {
        public const byte PduTypeData = 0x01;
        public const byte PduTypeDataAck = 0x02;
        public const byte Version01 = 0x01;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public double MessageId { get; set; }
        public byte Flags { get; set; }
        public byte ErrorCode { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }
        public byte[] Data { get; set; }
        public object AlternativePayload { get; set; }
    }
}
