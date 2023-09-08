using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Equivalent of TCP client socket factory that provides <see cref="StandardQuasiHttpClient"/> instances
    /// with client connections for sending quasi http requests to servers at remote endpoints.
    /// </summary>
    public interface IQuasiHttpClientTransport : IQuasiHttpTransport
    {
        /// <summary>
        /// Creates a connection to a remote endpoint.
        /// </summary>
        /// <param name="remoteEndpoint">the target endpoint of the connection
        /// allocation request</param>
        /// <param name="sendOptions">any options given to one of the Send*() methods of
        /// the <see cref="StandardQuasiHttpClient"/> class</param>
        /// <returns>a task whose result is ready for use as a duplex
        /// stream of data for reading and writing</returns>
        Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions);

        /// <summary>
        /// Returns an equivalent byte stream which can be used even
        /// after the release of a connection.
        /// </summary>
        /// <remarks>
        /// An implementation can decide to return the same stream if it
        /// determines that it is already independent of a connection, such as
        /// if the stream is in memory.
        /// </remarks>
        /// <param name="connection">connection that response body to buffer
        /// was retrieved from</param>
        /// <param name="body">response body to buffer in memory</param>
        /// <returns>a task whose result is an equivalent in-memory byte stream,
        /// or a byte stream which can be used after the release of
        /// the connection argument.</returns>
        Task<Stream> ApplyResponseBuffering(IQuasiHttpConnection connection,
            Stream body);

        /// <summary>
        /// Releases resources held by a connection of a quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to release</param>
        /// <param name="responseStreamingEnabled">whether response body
        /// still needs the connection to some extent</param>
        Task ReleaseConnection(IQuasiHttpConnection connection,
            bool responseStreamingEnabled);
    }
}
