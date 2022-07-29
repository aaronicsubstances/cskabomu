using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides helper functions for common operations performed by networking protocols on or with byte arrays,
    /// such as conversion to and from integers.
    /// </summary>
    public static class ByteUtils
    {
        /// <summary>
        /// Determines whether a given byte buffer slice is valid. A byte buffer slice is valid if and only if its
        /// backing byte array is not null and the values of its offset and (offset + length - 1) are valid
        /// indices in the backing byte array.
        /// </summary>
        /// <param name="data">backing byte array of slice. Invalid if null.</param>
        /// <param name="offset">offset of range in byte array. Invalid if negative.</param>
        /// <param name="length">length of range in byte array. Invalid if negative.</param>
        /// <returns>true if byte buffer slice is valid; false if otherwise.</returns>
        public static bool IsValidByteBufferSlice(byte[] data, int offset, int length)
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

        /// <summary>
        /// Converts a string to bytes in UTF-8 encoding.
        /// </summary>
        /// <param name="s">the string to convert</param>
        /// <returns>byte array representing UTF-8 encoding of string</returns>
        public static byte[] StringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        /// <summary>
        /// Creates a string from its UTF-8 encoding in a byte buffer slice.
        /// </summary>
        /// <param name="data">backing byte array of slice containing UTF-8 encoding</param>
        /// <param name="offset">offset of slice in data</param>
        /// <param name="length">length of slice in data</param>
        /// <returns>string equivalent of byte buffer slice containing UTF-8 encoding</returns>
        public static string BytesToString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        /// <summary>
        /// Creates a string from its hexadecimal encoding in a byte buffer slice.
        /// </summary>
        /// <remarks>
        /// The resulting string has an even length and uses the hexadecimal digits a-f instead of
        /// the uppercase A-F, for similarity with other platforms (NodeJS, Java, Python, etc).
        /// </remarks>
        /// <param name="data">backing byte array of slice containing hexadecimal encoding</param>
        /// <param name="offset">offset of slice in data</param>
        /// <param name="len">length of slice in data</param>
        /// <returns>string equivalent of byte buffer slice containing hexadecimal encoding</returns>
        public static string ConvertBytesToHex(byte[] data, int offset, int len)
        {
            // send out lower case and ensure even length.
            return BitConverter.ToString(data, offset, len).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Converts a hexadecimal string to its equivalent byte representation.
        /// </summary>
        /// <param name="hex">the hexadecimal string to convert.</param>
        /// <returns>byte array equivalent of hexadecimal string.</returns>
        public static byte[] ConvertHexToBytes(string hex)
        {
            // ensure even number of characters.
            if (hex.Length % 2 != 0)
            {
                hex = "0" + hex;
            }
            int charCount = hex.Length;
            byte[] rawBytes = new byte[charCount / 2];
            for (int i = 0; i < charCount; i += 2)
            {
                // accept both upper and lower case hex chars.
                rawBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return rawBytes;
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its big-endian representation and stores any specified number of
        /// the least significant bytes of the representation in a byte array.
        /// </summary>
        /// <param name="v">64-bit signed integer to convert.</param>
        /// <param name="rawBytes">destination buffer of conversion.</param>
        /// <param name="offset">offset into destination buffer to store the conversion.</param>
        /// <param name="length">the number of least significant bytes of the representation to store in the destination buffer (0-8).</param>
        /// <exception cref="T:System.ArgumentException">The <paramref name="offset"/> argument is negative.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="length"/> argument is negative or larger than 8.</exception>
        public static void SerializeUpToInt64BigEndian(long v, byte[] rawBytes, int offset, int length)
        {
            if (offset < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(length));
            }
            if (length > 8)
            {
                throw new ArgumentException("cannot be larger than 8", nameof(length));
            }
            int nextIndex = offset + length - 1;
            int shiftCount = 0;
            while (nextIndex >= offset)
            {
                rawBytes[nextIndex--] = (byte)(0xff & (v >> shiftCount));
                shiftCount += 8;
            }
        }

        /// <summary>
        /// Converts a 16-bit signed integer to its big-endian representation.
        /// </summary>
        /// <param name="v">16-bit signed integer to convert.</param>
        /// <returns>a byte array containing big-endian representation of integer argument</returns>
        public static byte[] SerializeInt16BigEndian(short v)
        {
            byte[] rawBytes = new byte[2];
            SerializeUpToInt64BigEndian(v, rawBytes, 0, rawBytes.Length);
            return rawBytes;
        }

        /// <summary>
        /// Converts a 32-bit signed integer to its big-endian representation.
        /// </summary>
        /// <param name="v">32-bit signed integer to convert.</param>
        /// <returns>a byte array containing big-endian representation of integer argument</returns>
        public static byte[] SerializeInt32BigEndian(int v)
        {
            byte[] rawBytes = new byte[4];
            SerializeUpToInt64BigEndian(v, rawBytes, 0, rawBytes.Length);
            return rawBytes;
        }

        /// <summary>
        /// Converts a 64-bit signed integer to its big-endian representation.
        /// </summary>
        /// <param name="v">64-bit signed integer to convert.</param>
        /// <returns>a byte array containing big-endian representation of integer argument</returns>
        public static byte[] SerializeInt64BigEndian(long v)
        {
            byte[] rawBytes = new byte[8];
            SerializeUpToInt64BigEndian(v, rawBytes, 0, rawBytes.Length);
            return rawBytes;
        }

        /// <summary>
        /// Creates a 64-bit signed integer from its big-endian representation, given any number of its least significant bytes.
        /// </summary>
        /// <param name="rawBytes">source buffer for conversion.</param>
        /// <param name="offset">the start of the data for the integer in the source buffer</param>
        /// <param name="length">the number of least significant bytes of the representation to fetch from the source buffer (0-8).</param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="offset"/> argument is negative.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="length"/> argument is negative or larger than 8.</exception>
        public static long DeserializeUpToInt64BigEndian(byte[] rawBytes, int offset, int length)
        {
            if (offset < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(length));
            }
            if (length > 8)
            {
                throw new ArgumentException("cannot be larger than 8", nameof(length));
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

        /// <summary>
        /// Creates a 16-bit signed integer from its big-endian representation.
        /// </summary>
        /// <param name="rawBytes">source buffer containing big-endian representation.</param>
        /// <param name="offset">the start of the data for the integer in the source buffer</param>
        /// <returns>16-bit integer equivalent of big-endian representation in source buffer</returns>
        public static short DeserializeInt16BigEndian(byte[] rawBytes, int offset)
        {
            return (short)DeserializeUpToInt64BigEndian(rawBytes, offset, 2);
        }

        /// <summary>
        /// Creates a 32-bit signed integer from its big-endian representation.
        /// </summary>
        /// <param name="rawBytes">source buffer containing big-endian representation.</param>
        /// <param name="offset">the start of the data for the integer in the source buffer</param>
        /// <returns>32-bit integer equivalent of big-endian representation in source buffer</returns>
        public static int DeserializeInt32BigEndian(byte[] rawBytes, int offset)
        {
            return (int)DeserializeUpToInt64BigEndian(rawBytes, offset, 4);
        }

        /// <summary>
        /// Creates a 64-bit signed integer from its big-endian representation.
        /// </summary>
        /// <param name="rawBytes">source buffer containing big-endian representation.</param>
        /// <param name="offset">the start of the data for the integer in the source buffer</param>
        /// <returns>64-bit integer equivalent of big-endian representation in source buffer</returns>
        public static long DeserializeInt64BigEndian(byte[] rawBytes, int offset)
        {
            return DeserializeUpToInt64BigEndian(rawBytes, offset, 8);
        }

        /// <summary>
        /// Computes the total count of bytes indicated by a collection of byte buffer slices.
        /// </summary>
        /// <param name="slices">array of byte buffer slices. each element must be non null and have a 
        /// non negative length.</param>
        /// <returns>sum of all lengths indicated by slices</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="slices"/> argument contains null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="slices"/> argument contains a negative length.</exception>
        public static int CalculateSizeOfSlices(ByteBufferSlice[] slices)
        {
            int byteCount = 0;
            foreach (var slice in slices)
            {
                if (slice == null)
                {
                    throw new ArgumentException("encountered null slice", nameof(slices));
                }
                if (slice.Length < 0)
                {
                    throw new ArgumentException("encountered slice with negative length", nameof(slices));
                }
                byteCount += slice.Length;
            }
            return byteCount;
        }
    }
}
