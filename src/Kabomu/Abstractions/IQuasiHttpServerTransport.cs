﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Equivalent of factory of sockets accepted from a TCP server socket, that provides
    /// <see cref="StandardQuasiHttpServer"/> instances
    /// with server operations for sending quasi http requests to servers at
    /// remote endpoints.
    /// </summary>
    public interface IQuasiHttpServerTransport : IQuasiHttpTransport
    {
        /// <summary>
        /// Releases resources held by a connection of a quasi http transport instance.
        /// </summary>
        /// <param name="connection">the connection to release</param>
        Task ReleaseConnection(IQuasiHttpConnection connection);
    }
}
