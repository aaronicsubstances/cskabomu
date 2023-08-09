using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Represents connections of TCP and any network protocol 
    /// or IPC mechanism which is connection-oriented like TCP, 
    /// where duplex streams of data are provided in the form of connections for reading and writing simulataneously.
    /// </summary>
    /// <remarks>
    /// The expectations for implementations are that
    /// <list type="number">
    /// <item>writes, reads and connection releases must be implemented to allow for
    /// calls from different threads in a thread-safe manner.
    /// <para>It is acceptable however, if an implementation chooses not to support concurrent
    /// multiple writes or concurrent multiple reads.
    /// </para>
    /// </item>
    /// <item>multiple calls to connection releases must be tolerated.</item>
    /// </list>
    /// </remarks>
    public interface IQuasiHttpTransport
    {
        /// <summary>
        /// Gets a reader which can be used to write data to a connection of
        /// this quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to write to</param>
        /// <returns>a writer which can be used to write bytes to the connection argument</returns>
        object GetWriter(object connection);

        /// <summary>
        /// Gets a reader which can be used to read data from a connection of
        /// this quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to read from</param>
        /// <returns>a reader which can be used to read bytes from the connection argument</returns>
        object GetReader(object connection);

        /// <summary>
        /// Releases or closes a connection of this quasi http transport instance, ensuring that
        /// subsequent reads and writes to the connection will fail.
        /// </summary>
        /// <param name="connection">the connection to release or close</param>
        /// <returns>a task representing the asynchronous connection release operation</returns>
        Task ReleaseConnection(object connection);
    }
}
