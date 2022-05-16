using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Examples.Common
{
    public class LocalhostUdpDatagram
    {
        public static readonly byte Version01 = 1;

        public static readonly byte PduTypeSyn = 1;
        public static readonly byte PduTypeSynAck = 2;
        public static readonly byte PduTypeData = 3;
        public static readonly byte PduTypeFin = 4;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
        public string ConnectionId { get; set; }
        public byte[] Data { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }

        public static LocalhostUdpDatagram Deserialize(byte[] data, int offset, int length)
        {
            var pdu = new LocalhostUdpDatagram();
            pdu.Version = data[offset + 0];
            pdu.PduType = data[offset + 1];
            pdu.Flags = data[offset + 2];
            var connectionIdLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(data, offset + 3, 1);
            int headerLen = 4 + connectionIdLength;
            if (headerLen > length)
            {
                throw new ArgumentException("invalid pdu");
            }
            pdu.ConnectionId = ByteUtils.BytesToString(data, offset + 4, connectionIdLength);
            pdu.Data = data;
            pdu.DataOffset = offset + headerLen;
            pdu.DataLength = length - headerLen;
            return pdu;
        }

        public byte[] Serialize()
        {
            var connectionIdBytes = ByteUtils.StringToBytes(ConnectionId);
            if (connectionIdBytes.Length > 255)
            {
                throw new Exception("connection id must consist of 255 or less bytes");
            }
            var pduBytes = new byte[connectionIdBytes.Length + 4 + DataLength];
            pduBytes[0] = Version;
            pduBytes[1] = PduType;
            pduBytes[2] = Flags;
            ByteUtils.SerializeUpToInt64BigEndian(connectionIdBytes.Length, pduBytes, 3, 1);
            Array.Copy(connectionIdBytes, 0, pduBytes, 4, connectionIdBytes.Length);
            if (DataLength > 0)
            {
                Array.Copy(Data, DataOffset, pduBytes, 4 + connectionIdBytes.Length, DataLength);
            }
            return pduBytes;
        }
    }
}
