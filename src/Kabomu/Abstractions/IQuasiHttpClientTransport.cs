﻿using System;
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
        /// Releases resources held by a connection of a quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to release</param>
        /// <param name="responseStreamingEnabled">whether response body
        /// still needs the connection to some extent</param>
        Task ReleaseConnection(IQuasiHttpConnection connection,
            bool responseStreamingEnabled);
    }
}