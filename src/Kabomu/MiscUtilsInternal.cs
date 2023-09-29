using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    /// <summary>
    /// Any wrapper of standard library function other than I/O is placed here.
    /// </summary>
    internal static class MiscUtilsInternal
    {
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

        /// <summary>
        /// Parses a string as a valid 32-bit signed integer.
        /// </summary>
        /// <param name="input">the string to parse. Can be surrounded by
        /// whitespace</param>
        /// <returns>valid 32-bit integer</returns>
        /// <exception cref="FormatException">if an error occurs</exception>
        public static int ParseInt32(string input)
        {
            return int.Parse(input);
        }

        /// <summary>
        /// Determines whether a given byte buffer slice is valid.
        /// A byte buffer slice is valid if and only if its
        /// backing byte array is not null and the values of
        /// its offset and (offset + length - 1) are valid
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

        public static string BytesToString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        public static int GetByteCount(string v)
        {
            return Encoding.UTF8.GetByteCount(v);
        }

        public static byte[] ConcatBuffers(List<byte[]> chunks)
        {
            if (chunks.Count == 1)
            {
                return chunks[0];
            }
            int totalLen = chunks.Sum(c => c.Length);
            byte[] result = new byte[totalLen];
            int offset = 0;
            foreach (var chunk in chunks)
            {
                Array.Copy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }
            return result;
        }

        /// <summary>
        /// Provides equivalent functionality to Promise.all() of NodeJS
        /// </summary>
        /// <param name="candiates">tasks to race</param>
        /// <returns>asynchronous result which represents successful
        /// end of all arguments, or failure of one of them</returns>
        public static async Task WhenAnyFailOrAllSucceed(List<Task> candiates)
        {
            var newList = new List<Task>(candiates);
            while (newList.Count > 0)
            {
                var t = await Task.WhenAny(newList);
                await t;
                newList.Remove(t);
            }
        }

        /// <summary>
        /// Copied from
        /// https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types
        /// </summary>
        public static IAsyncResult AsApm<T>(this Task<T> task,
            AsyncCallback callback, object state)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            var tcs = new TaskCompletionSource<T>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                if (callback != null)
                    callback(tcs.Task);
            }, TaskScheduler.Default);
            return tcs.Task;
        }

        public static IAsyncResult AsApm(this Task task,
            AsyncCallback callback, object state)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            var tcs = new TaskCompletionSource(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult();

                if (callback != null)
                    callback(tcs.Task);
            }, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}
