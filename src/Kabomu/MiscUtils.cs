using Kabomu.Exceptions;
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
    /// Provides helper functions for common operations performed by
    /// during custom chunk protocol processing.
    /// </summary>
    public static class MiscUtils
    {
        /// <summary>
        /// The limit of data buffering when reading byte streams into memory. Equal to 128 MB.
        /// </summary>
        private static readonly int DefaultDataBufferLimit = 134_217_728;

        /// <summary>
        /// The default read buffer size. Equal to 8,192 bytes.
        /// </summary>
        private static readonly int DefaultReadBufferSize = 8_192;

        private static byte[] AllocateReadBuffer()
        {
            return new byte[DefaultReadBufferSize];
        }

        /// <summary>
        /// Reads in data in order to completely fill in a buffer.
        /// </summary>
        /// <param name="inputStream">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="length">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an error.</param>
        /// <exception cref="CustomIOException">Not enough bytes were found in
        /// <paramref name="inputStream"/> argument to supply requested number of bytes.</exception>
        public static void ReadExactBytes(Stream inputStream,
            byte[] data, int offset, int length)
        {
            // allow zero-byte reads to proceed to touch the
            // stream, rather than just return.
            while (true)
            {
                int bytesRead = inputStream.Read(data, offset, length);
                if (bytesRead > length)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {length})");
                }
                if (bytesRead < length)
                {
                    if (bytesRead <= 0)
                    {
                        throw new CustomIOException("unexpected end of read");
                    }
                    offset += bytesRead;
                    length -= bytesRead;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Reads in data in order to completely fill in a buffer.
        /// </summary>
        /// <param name="inputStream">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="length">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an error.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="CustomIOException">Not enough bytes were found in
        /// <paramref name="inputStream"/> argument to supply requested number of bytes.</exception>
        public static Task ReadExactBytesAsync(Stream inputStream,
            byte[] data, int offset, int length,
            CancellationToken cancellationToken = default)
        {
            return ReadExactBytesAsync(inputStream,
                new Memory<byte>(data, offset, length),
                cancellationToken);
        }

        /// <summary>
        /// Reads in data in order to completely fill in a buffer.
        /// </summary>
        /// <param name="inputStream">source of bytes to read</param>
        /// <param name="data">destination buffer</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="CustomIOException">Not enough bytes were found in
        /// <paramref name="inputStream"/> argument to supply requested number of bytes.</exception>
        public static async Task ReadExactBytesAsync(Stream inputStream,
            Memory<byte> data,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // allow zero-byte reads to proceed to touch the
            // stream, rather than just return.
            int offset = 0, length = data.Length;
            while (true)
            {
                int bytesRead = await inputStream.ReadAsync(
                    data.Slice(offset, length),
                    cancellationToken);
                if (bytesRead > length)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {length})");
                }
                if (bytesRead < length)
                {
                    if (bytesRead <= 0)
                    {
                        throw new CustomIOException("unexpected end of read");
                    }
                    offset += bytesRead;
                    length -= bytesRead;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Reads in all of a reader's data into an in-memory buffer.
        /// </summary>
        /// <remarks>
        /// One can specify a maximum size beyond which an error will be
        /// thrown if there is more data after that limit.
        /// </remarks>
        /// <param name="reader">The source of data to read</param>
        /// <param name="bufferingLimit">Indicates the maximum size in bytes of the resulting buffer.
        /// Can pass zero to use default value of 128 MB. Can also pass a negative value which will ignore
        /// imposing a maximum size.</param>
        /// <returns>A task whose result is an in-memory stream which has all of the remaining data in the reader.</returns>
        /// <exception cref="CustomIOException">The <paramref name="bufferingLimit"/> argument indicates a positive value,
        /// and data in <paramref name="body"/> argument exceeded that value.</exception>
        public static async Task<MemoryStream> ReadAllBytes(Stream reader,
            int bufferingLimit = 0)
        {
            if (bufferingLimit == 0)
            {
                bufferingLimit = DefaultDataBufferLimit;
            }

            var byteStream = new MemoryStream();
            if (bufferingLimit < 0)
            {
                await CopyBytesToStream(reader, byteStream);
                return byteStream;
            }

            var readBuffer = AllocateReadBuffer();
            int totalBytesRead = 0;

            while (true)
            {
                int bytesToRead = Math.Min(readBuffer.Length, bufferingLimit - totalBytesRead);
                // force a read of 1 byte if there are no more bytes to read into memory stream buffer
                // but still remember that no bytes was expected.
                var expectedEndOfRead = false;
                if (bytesToRead == 0)
                {
                    bytesToRead = 1;
                    expectedEndOfRead = true;
                }
                int bytesRead = await reader.ReadAsync(readBuffer, 0, bytesToRead);
                if (bytesRead > bytesToRead)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {bytesToRead})");
                }
                if (bytesRead > 0)
                {
                    if (expectedEndOfRead)
                    {
                        throw CustomIOException.CreateDataBufferLimitExceededErrorMessage(
                            bufferingLimit);
                    }
                    byteStream.Write(readBuffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }
                else
                {
                    break;
                }
            }
            return byteStream;
        }

        /// <summary>
        /// Copies bytes from an input stream to an output stream.
        /// </summary>
        /// <param name="inputStream">source of data being transferred</param>
        /// <param name="outputStream">destination of data being transferred</param>
        public static async Task CopyBytesToStream(
            Stream inputStream, Stream outputStream)
        {
            await inputStream.CopyToAsync(outputStream);
        }

        /// <summary>
        /// Copies all remaining contents of an input stream into
        /// an byte sink function.
        /// </summary>
        /// <param name="inputStream">source of data to copy</param>
        /// <param name="sink">byte sink function</param>
        public static async Task CopyBytesToSink(Stream inputStream,
            Func<byte[], int, int, Task> sink, int readBufferSize = default,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (readBufferSize <= 0)
            {
                readBufferSize = DefaultReadBufferSize;
            }
            byte[] readBuffer = new byte[readBufferSize];

            while (true)
            {
                int bytesRead = await inputStream.ReadAsync(readBuffer,
                    cancellationToken);
                if (bytesRead > readBuffer.Length)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {readBuffer.Length})");
                }
                if (bytesRead > 0)
                {
                    await sink( readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
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

        public static string BytesToString(byte[] data)
        {
            return BytesToString(data, 0, data.Length);
        }

        internal static string BytesToString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        public static byte[] StringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        internal static string PadLeftWithZeros(string v, int totalLen)
        {
            return v.PadLeft(totalLen, '0');
        }

        public async static Task<T> CompleteMainTask<T>(
            Task<T> mainTask, params Task[] cancellationTasks)
        {
            if (mainTask == null)
            {
                throw new ArgumentNullException(nameof(mainTask));
            }

            await EliminateCancellationTasks(mainTask, cancellationTasks);
            return await mainTask;
        }

        public async static Task CompleteMainTask(
            Task mainTask, params Task[] cancellationTasks)
        {
            if (mainTask == null)
            {
                throw new ArgumentNullException(nameof(mainTask));
            }

            await EliminateCancellationTasks(mainTask, cancellationTasks);
            await mainTask;
        }

        private async static Task EliminateCancellationTasks(
            Task mainTask, Task[] cancellationTasks)
        {
            // ignore null tasks and successful results from
            // cancellation tasks.
            var tasks = new List<Task> { mainTask };
            foreach (var t in cancellationTasks)
            {
                if (t != null)
                {
                    tasks.Add(t);
                }
            }
            while (tasks.Count > 1)
            {
                var firstTask = await Task.WhenAny(tasks);
                if (firstTask == mainTask)
                {
                    break;
                }
                await firstTask; // let any exceptions bubble up.
                tasks.Remove(firstTask);
            }
        }

        /// <summary>
        /// Provides equivalent functionality to Promise.all() of NodeJS
        /// </summary>
        /// <param name="candiates">tasks</param>
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

        // Copied from
        // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types
        internal static IAsyncResult AsApm<T>(this Task<T> task,
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
    }
}
