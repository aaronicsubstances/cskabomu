using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public static class ByteUtils
    {
        public static bool IsValidMessagePayload(byte[] data, int offset, int length)
        {
            if (data == null)
            {
                return false;
            }
            if (offset < 0)
            {
                return false;
            }
            if (length < 0)
            {
                return false;
            }
            if (offset + length > data.Length)
            {
                return false;
            }
            return true;
        }

        public static byte[] StringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        public static string BytesToString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        public static string ConvertBytesToHex(byte[] data, int offset, int len)
        {
            // send out lower case for similarity with other platforms (Java, Python, NodeJS, etc)
            // ensure even length.
            return BitConverter.ToString(data, offset, len).Replace("-", "").ToLower();
        }

        public static byte[] ConvertHexToBytes(string hex)
        {
            int charCount = hex.Length;
            if (charCount % 2 != 0)
            {
                throw new Exception("arg must have even length");
            }
            byte[] rawBytes = new byte[charCount / 2];
            for (int i = 0; i < charCount; i += 2)
            {
                // accept both upper and lower case hex chars.
                rawBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return rawBytes;
        }

        public static void SerializeUpToInt64BigEndian(long v, byte[] rawBytes, int offset, int length)
        {
            if (length > 8)
            {
                length = 8;
            }
            int nextIndex = offset + length - 1;
            int shiftCount = 0;
            while (nextIndex >= offset)
            {
                rawBytes[nextIndex--] = (byte)(0xff & (v >> shiftCount));
                shiftCount += 8;
            }
        }

        public static byte[] SerializeInt16BigEndian(short v)
        {
            byte[] rawBytes = new byte[2];
            SerializeUpToInt64BigEndian(v, rawBytes, 0, rawBytes.Length);
            return rawBytes;
        }

        public static byte[] SerializeInt32BigEndian(int v)
        {
            byte[] rawBytes = new byte[4];
            SerializeUpToInt64BigEndian(v, rawBytes, 0, rawBytes.Length);
            return rawBytes;
        }

        public static byte[] SerializeInt64BigEndian(long v)
        {
            byte[] rawBytes = new byte[8];
            SerializeUpToInt64BigEndian(v, rawBytes, 0, rawBytes.Length);
            return rawBytes;
        }

        public static long DeserializeUpToInt64BigEndian(byte[] rawBytes, int offset, int length)
        {
            if (length > 8)
            {
                length = 8;
            }
            int nextIndex = offset + length - 1;
            int shiftCount = 0;
            long v = 0;
            while (nextIndex >= offset)
            {
                v |= (long)(rawBytes[nextIndex--] & 0xff) << shiftCount;
                shiftCount += 8;
            }
            return v;
        }

        public static short DeserializeInt16BigEndian(byte[] rawBytes, int offset)
        {
            int intSize = 2;
            return (short)DeserializeUpToInt64BigEndian(rawBytes, offset, intSize);
        }

        public static int DeserializeInt32BigEndian(byte[] rawBytes, int offset)
        {
            int intSize = 4;
            return (int)DeserializeUpToInt64BigEndian(rawBytes, offset, intSize);
        }

        public static long DeserializeInt64BigEndian(byte[] rawBytes, int offset)
        {
            int intSize = 8;
            return DeserializeUpToInt64BigEndian(rawBytes, offset, intSize);
        }

        public static int CalculateSizeOfSlices(ByteBufferSlice[] slices)
        {
            int byteCount = 0;
            foreach (var slice in slices)
            {
                byteCount += slice.Length;
            }
            return byteCount;
        }
    }
}
