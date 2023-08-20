using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides helper functions for common operations performed by
    /// networking protocols on or with byte arrays, such as conversion to and
    /// from integers.
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
        /// <returns>true if and only if byte buffer slice is valid.</returns>
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
        /// Creates a string from its UTF-8 encoding in a byte array.
        /// </summary>
        /// <param name="data">byte array of UTF-8 encoded data</param>
        /// <returns>string equivalent of byte buffer</returns>
        public static string BytesToString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
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
        /// Converts a 32-bit signed integer to its big-endian representation and stores any specified number of
        /// the least significant bytes of the representation in a byte array.
        /// </summary>
        /// <param name="v">32-bit signed integer to convert.</param>
        /// <param name="rawBytes">destination buffer of conversion.</param>
        /// <param name="offset">offset into destination buffer to store the conversion.</param>
        /// <param name="length">the number of least significant bytes of the representation to store in the destination buffer (0-4).</param>
        /// <exception cref="T:System.ArgumentException">The <paramref name="offset"/> argument is negative.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="length"/> argument is negative or larger than 4.</exception>
        public static void SerializeUpToInt32BigEndian(int v, byte[] rawBytes, int offset, int length)
        {
            if (offset < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(length));
            }
            if (length > 4)
            {
                throw new ArgumentException("cannot be larger than 4", nameof(length));
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
        /// Creates a 32-bit signed integer from its big-endian representation, given any number of its least significant bytes.
        /// </summary>
        /// <param name="rawBytes">source buffer for conversion.</param>
        /// <param name="offset">the start of the data for the integer in the source buffer</param>
        /// <param name="length">the number of least significant bytes of the representation to fetch from the source buffer (0-4).</param>
        /// <param name="signed">whether the bytes should be interpreted as a signed integer. pass false to interpret as an
        /// unsigned integer. NB: for length of 4 bytes, interpretation is always as a signed integer.</param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="offset"/> argument is negative.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="length"/> argument is negative or larger than 4.</exception>
        public static int DeserializeUpToInt32BigEndian(byte[] rawBytes, int offset, int length,
            bool signed)
        {
            if (offset < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(length));
            }
            if (length > 4)
            {
                throw new ArgumentException("cannot be larger than 4", nameof(length));
            }
            int nextIndex = offset + length - 1;
            int shiftCount = 0;
            int v = 0;
            while (nextIndex >= offset)
            {
                v |= (rawBytes[nextIndex--] & 0xff) << shiftCount;
                shiftCount += 8;
            }
            // cater for signed integers of size less than 4 bytes (those exactly
            // of 4 bytes will already be signed).
            if (signed && length > 0 && length < 4 && (rawBytes[offset] >> 7) != 0)
            {
                int inverter = ~((1 << (length * 8)) - 1);
                v = v | inverter;
            }
            return v;
        }

        /// <summary>
        /// Parses a string as a valid 48-bit signed integer.
        /// </summary>
        /// <param name="input">the string to parse. Can be surrounded by
        /// whitespace</param>
        /// <returns>verified 48-bit integer</returns>
        /// <exception cref="FormatException">if an error occurs</exception>
        public static long ParseInt48(string input)
        {
            var n = long.Parse(input);
            if (n < -140_737_488_355_328L || n > 140_737_488_355_327L)
            {
                throw new FormatException("invalid 48-bit integer: " + input);
            }
            return n;
        }
    }
}
