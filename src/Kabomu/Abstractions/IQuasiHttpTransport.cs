using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents connection manager for TCP and any network protocol 
    /// or IPC mechanism which is connection-oriented like TCP, 
    /// where duplex streams of data are provided in the form of connections for
    /// reading and writing simulataneously.
    /// </summary>
    /// <remarks>
    /// The expectations for implementations are that
    /// <list type="number">
    /// <item>an implementation does not bother to protect 
    /// writes, reads and connection releases from thread interference
    /// (memory consistency is still a must).
    /// <para>an implementation does not bother to support concurrent
    /// multiple writes or concurrent multiple reads.
    /// </para>
    /// </item>
    /// <item>concurrent calls to connection releases must be tolerated,
    /// which could possibly be concurrent with ongoing reads and writes.</item>
    /// </list>
    /// </remarks>
    public interface IQuasiHttpTransport
    {
        /// <summary>
        /// Gets a writer acceptable by <see cref="QuasiHttpUtils.WriteBytes"/>,
        /// which can be used to write data to a connection of
        /// a quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection associated with the writer</param>
        /// <returns>a writer which can be used to write bytes to the connection argument</returns>
        object GetWriter(IQuasiHttpConnection connection);

        /// <summary>
        /// Gets a reader acceptable by <see cref="QuasiHttpUtils.ReadBytes"/>,
        /// which can be used to read data from a connection of
        /// a quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection associated with the reader</param>
        /// <returns>a reader which can be used to read bytes from the connection argument</returns>
        object GetReader(IQuasiHttpConnection connection);

        /// <summary>
        /// Releases resources held by a connection of a quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to release</param>
        /// <returns>a task representing the asynchronous connection release operation</returns>
        Task ReleaseConnection(IQuasiHttpConnection connection);
        Task Write(IQuasiHttpConnection connection, bool isResponse,
            byte[] encodedHeaders, object requestBodyReader);
        Task<IEncodedReadRequest> Read(IQuasiHttpConnection connection,
            bool isResponse);
    }
}
