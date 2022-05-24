using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public static class ByteUtils
    {
        public static void ReadBytesFully(IQuasiHttpTransport transport,
            object connection, byte[] data, int offset, int bytesToRead, Action<Exception> cb)
        {
            HandlePartialReadOutcome(transport, connection, data, offset, bytesToRead, null, 0, cb);
        }

        private static void HandlePartialReadOutcome(IQuasiHttpTransport transport,
           object connection, byte[] data, int offset, int bytesToRead, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead < bytesToRead)
            {
                int newOffset = offset + bytesRead;
                int newBytesToRead = bytesToRead - bytesRead;
                transport.ReadBytes(connection, data, newOffset, newBytesToRead, (e, bytesRead) =>
                   HandlePartialReadOutcome(transport, connection, data, newOffset, newBytesToRead, e, bytesRead, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

        public static void TransferBodyToTransport(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            Action<Exception> cb)
        {
            byte[] buffer = new byte[transport.MaxMessageOrChunkSize];
            HandleWriteOutcome(transport, connection, body, buffer, null, cb);
        }

        private static void HandleWriteOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            byte[] buffer, Exception e, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            body.OnDataRead(buffer, 0, buffer.Length, (e, bytesRead) =>
                HandleReadOutcome(transport, connection, body, buffer, e, bytesRead, cb));
        }

        private static void HandleReadOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            byte[] buffer, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead > 0)
            {
                transport.WriteBytesOrSendMessage(connection, buffer, 0, bytesRead, e =>
                    HandleWriteOutcome(transport, connection, body, buffer, e, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

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
    }
}
