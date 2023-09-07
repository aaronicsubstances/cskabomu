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
        /// Releases resources held by a connection of a quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to release</param>
        Task ReleaseConnection(IQuasiHttpConnection connection);
        Task Write(IQuasiHttpConnection connection, bool isResponse,
            IEncodedQuasiHttpEntity entity);
        Task<IEncodedQuasiHttpEntity> Read(IQuasiHttpConnection connection,
            bool isResponse);
    }
}
