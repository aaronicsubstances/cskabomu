using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents a byte stream which forms part of a quasi HTTP request or response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations must operate without concurrency bugs, but should try to employ non-blocking synchronization
    /// as much as possible to yield high performance during usage.
    /// </para>
    /// <para>
    /// To help achieve this, this interface imposes a restriction on its usage: calls to the ReadBytes() method
    /// can only be made from 1 thread at a time; interleaved calls from multiple threads are NOT
    /// allowed. Hence implementations can take advantage of this restriction to optimize their code.
    /// </para>
    /// <para>
    /// The EndRead() method however, does not have this restriction, and so implementations must ensure interleaved
    /// calls from multiple threads work correctly there.
    /// </para>
    /// <para>
    /// It is acceptable that an implementation fails a ReadBytes() call, while a previous ReadBytes() call
    /// from the same thread has not completed. Also it is acceptable that an EndRead() does nothing to interrupt any
    /// ongoing call to ReadBytes(). The default implementations in the Kabomu library demonstrate the use of 
    /// these provisions and restrictions.
    /// </para>
    /// </remarks>
    public interface IQuasiHttpBody
    {
        /// <summary>
        /// Gets the number of bytes in the stream represented by this instance, or -1 (actually any negative value)
        /// to indicate an unknown number of bytes.
        /// </summary>
        long ContentLength { get; }

        /// <summary>
        /// Returns any string or null which can be used by the receiving end of the bytes generated
        /// by this instance, to determine how to interpret the bytes. It is equivalent to "Content-Type" header
        /// in HTTP.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Reads in a specified number of bytes from the underlying byte stream of an instance.
        /// </summary>
        /// <param name="data">the destination buffer of the bytes to be read.</param>
        /// <param name="offset">the starting position in the destination buffer for saving the bytes to be read.</param>
        /// <param name="bytesToRead">the number of bytes to read</param>
        /// <returns>a task whose result is a nonnegative integer indicating the number of bytes actually read</returns>
        /// <exception cref="ArgumentException">Either destination buffer is null, or the arguments provided 
        /// generate invalid positions in destination buffer.</exception>
        /// <exception cref="EndOfReadException">EndRead() method has been called.</exception>
        Task<int> ReadBytes(byte[] data, int offset, int bytesToRead);

        /// <summary>
        /// Ends the read by disposing off any resources being used by underlying byte stream,
        /// and ensuring that subsequent calls to Read() fail.
        /// </summary>
        /// <returns>task representing asynchronous execution of the call</returns>
        Task EndRead();
    }
}
