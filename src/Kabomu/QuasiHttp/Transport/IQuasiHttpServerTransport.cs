using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Equivalent of TCP server sockets that provides <see cref="Server.IQuasiHttpServer"/> instances
    /// with server operations for sending quasi http requests to servers or remote endpoints.
    /// </summary>
    public interface IQuasiHttpServerTransport : IQuasiHttpTransport
    {
        /// <summary>
        /// Gets and sets a mutex object which will most likely be needed to synchronize server operations.
        /// <para>
        /// This property is exposed publicly to allow frameworks employing a general concurrency mechanism
        /// to impose their policy through this property.
        /// </para>
        /// </summary>
        IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Performs any startup operations required for receiving of connections to succeed. E.g. TCP servers will
        /// bind and listen at this point.
        /// </summary>
        /// <remarks>
        /// Implementations should ignore rather than fail start calls to an already started server.
        /// </remarks>
        /// <returns>a task representing the asynchronous operation</returns>
        Task Start();

        /// <summary>
        /// Performs any shutdown operations required to stop the ongoing and future receiving of connections. E.g. TCP
        /// servers will be closed and diposed off at this point.
        /// </summary>
        /// <remarks>
        /// Implementations should ignore rather than fail stop calls to an already stopped server.
        /// </remarks>
        /// <returns>a task representing the asynchronous operation</returns>
        Task Stop();

        /// <summary>
        /// Returns true if and only if the server has been started and has not been stopped, ie is running.
        /// </summary>
        /// <returns>a task whose result indicates whether the server is running</returns>
        Task<bool> IsRunning();

        /// <summary>
        /// Waits for any incoming connection. Equivalent to TCP's accept() operation. This operation should only
        /// succeed if and only if the server is running.
        /// <para>
        /// Implementations can also supply connection-related or server-related environment variables which
        /// can provide information for interested consumsers. E.g. when used with <see cref="Server.StandardQuasiHttpServer"/>
        /// instances, the variables will be included in a quasi http request environment and passed on to
        /// quasi web applications for processing.
        /// </para>
        /// </summary>
        /// <returns>A task whose result will contain a connection ready to use as a duplex
        /// stream of data for reading and writing</returns>
        Task<IConnectionAllocationResponse> ReceiveConnection();
    }
}
