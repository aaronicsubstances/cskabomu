using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Represents connection manager for TCP and any network protocol 
    /// or IPC mechanism which is connection-oriented like TCP, 
    /// where duplex streams of data are provided in the form of connections for reading and writing simulataneously.
    /// </summary>
    /// <remarks>
    /// The expectations for implementations are that
    /// <list type="number">
    /// <item>writes, reads and connection releases must be implemented to allow for
    /// memory consistency (but not necessarily protection from thread interference).
    /// <para>an implementation does not bother to support concurrent
    /// multiple writes or concurrent multiple reads.
    /// </para>
    /// </item>
    /// <item>multiple calls to connection releases must be tolerated.</item>
    /// </list>
    /// </remarks>
    public interface IQuasiHttpTransport
    {
        /// <summary>
        /// Gets a writer acceptable by <see cref="IOUtils.WriteBytes"/>,
        /// which can be used to write data to a connection of
        /// this quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to write to</param>
        /// <returns>a writer which can be used to write bytes to the connection argument</returns>
        object GetWriter(object connection);

        /// <summary>
        /// Gets a reader acceptable by <see cref="IOUtils.ReadBytes"/>,
        /// which can be used to read data from a connection of
        /// this quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to read from</param>
        /// <returns>a reader which can be used to read bytes from the connection argument</returns>
        object GetReader(object connection);

        /// <summary>
        /// Releases resources held by a connection of this quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to release</param>
        /// <returns>a task representing the asynchronous connection release operation</returns>
        Task ReleaseConnection(object connection);
    }
}
