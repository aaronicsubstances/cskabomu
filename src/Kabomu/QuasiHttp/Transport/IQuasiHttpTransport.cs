using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Represents TCP and any connection-oriented network protocol or IPC mechanism which resembles TCP, 
    /// where duplex streams of data are provided in the form of connections for reading and writing simulataneously.
    /// </summary>
    /// <remarks>
    /// The expectations for implementations are that
    /// <list type="number">
    /// <item>writes, reads and connection releases must be implemented to allow for concurrent calls from different threads
    /// in a thread-safe manner.
    /// <para>It is acceptable however, if an implementation chooses not to support concurrent
    /// multiple writes or concurrent multiple reads.
    /// </para>
    /// </item>
    /// <item>connection releases must cause any future write or read requests to fail. Ongoing reads or write requests
    /// may also fail as well depending on implementation</item>
    /// <item>multiple calls to connection releases must be tolerated.</item>
    /// </list>
    /// </remarks>
    public interface IQuasiHttpTransport
    {
        /// <summary>
        /// Writes data to a connection of this quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to write to</param>
        /// <param name="data">the source byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to write</param>
        /// <returns>a task representing the asynchronous write operation</returns>
        Task WriteBytes(object connection, byte[] data, int offset, int length);

        /// <summary>
        /// Reads data from a connection of this quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to read from</param>
        /// <param name="data">the destination byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>a task representing the asynchronous read operation, whose result will
        /// be the number of bytes actually read. May be zero or less than the number of bytes requested
        /// depending on the implementation of the transport.</returns>
        Task<int> ReadBytes(object connection, byte[] data, int offset, int length);

        /// <summary>
        /// Releases or closes a connection of this quasi http transport instance, ensuring that
        /// subsequent reads and writes to the connection will fail.
        /// </summary>
        /// <param name="connection">the connection to release or close</param>
        /// <returns>a task representing the asynchronous connection release operation</returns>
        Task ReleaseConnection(object connection);

        /// <summary>
        /// Meant to be used to efficiently write out certain kinds of quasi http bodies with 
        /// positive content lengths. Typical example is sending files efficiently on localhost.
        /// </summary>
        /// <remarks>
        /// If an implementation chooses to write out a body, it must first write out given blob of bytes,
        /// then serialize the body onto a connection handle, and then return true afterwards.
        /// <para>
        /// Implementations which are not interested in handling bodies directly should simply return false.
        /// </para>
        /// </remarks>
        /// <param name="connection">a network handle with which writing can be done</param>
        /// <param name="prefix">an opaque blob which should first be written out if it is decided
        /// that the body should be seriarlized.</param>
        /// <param name="body">body to serialize</param>
        /// <returns>a task whose result is true if body was written out, and false if no writing occured.</returns>
        Task<bool> TrySerializeBody(object connection, byte[] prefix, IQuasiHttpBody body);

        /// <summary>
        /// Counterpart to <see cref="TrySerializeBody"/> for efficiently retrieving
        /// certain kinds of quasi http bodies with positive content length.
        /// </summary>
        /// <remarks>
        /// Once this method is called, an implementation must provide a non-null quasi http body.
        /// </remarks>
        /// <param name="connection">a network handle with which writing can be done</param>
        /// <param name="contentLength">length of quasi http body to retrieve.</param>
        /// <returns>task whose result is a quasi http body deserialized from a connection handle.</returns>
        Task<IQuasiHttpBody> DeserializeBody(object connection, long contentLength);
    }
}
