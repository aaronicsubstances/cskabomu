using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.MemoryBasedTransport
{
    /// <summary>
    /// Implements the standard in-memory client-side quasi http transport provided by the
    /// Kabomu library, which can act both connection-oriented mode and alternative transport mode.
    /// </summary>
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport, IQuasiHttpAltTransport
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MemoryBasedClientTransport"/> class.
        /// </summary>
        public MemoryBasedClientTransport()
        {
        }

        /// <summary>
        /// Gets or sets the endpoint which should identify an instance of this class.
        /// </summary>
        public object LocalEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the virtual hub of servers connected to an instance of this class. Direct request processing
        /// and indirect request proessing via connection allocation are both done through this dependency.
        /// </summary>
        public IMemoryBasedTransportHub Hub { get; set; }

        /// <summary>
        /// Gets or sets the maximum write buffer limit for connections which will be allocated by
        /// this class. A positive value means that
        /// any attempt to write (excluding last writes) such that the total number of
        /// bytse outstanding tries to exceed that positive value, will result in an instance of the
        /// <see cref="DataBufferLimitExceededException"/> class to be thrown.
        /// <para></para>
        /// By default this property is zero, and so indicates that the default value of 65,6536 bytes
        /// will be used as the maximum write buffer limit.
        /// </summary>
        public int MaxWriteBufferLimit { get; set; }

        /// <summary>
        /// Processes send requests directly by forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="request">the quasi http request to send.</param>
        /// <param name="connectivityParams">server endpoint information as required by <see cref="Hub"/> dependency</param>
        /// <returns>a pair whose first item is a task whose result will be the quasi http response
        /// processed by this tranport instance; and whose second task is always null to indicate that
        /// this class does not support cancellation requests.</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(IQuasiHttpRequest request,
            IConnectivityParams connectivityParams)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            var resTask = hub.ProcessSendRequest(this, connectivityParams, request);
            object sendCancellationHandle = null;
            return (resTask, sendCancellationHandle);
        }

        /// <summary>
        /// This implementation of the <see cref="IQuasiHttpAltTransport"/> type does nothing
        /// about cancellation of direct send requests.
        /// </summary>
        /// <param name="sendCancellationHandle">will be ignored.</param>
        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }

        /// <summary>
        /// Allocates connections by forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connectivityParams">server endpoint information as required by <see cref="Hub"/> dependency</param>
        /// <returns>a task whose result will contain a connection ready for use as a duplex
        /// stream of data for reading and writing</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.AllocateConnection(this, connectivityParams);
        }

        /// <summary>
        /// Releases a connection allocated by a instance of this class by
        /// forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connection">connection to release. null, invalid and already released connections are ignored.</param>
        /// <returns>task representing asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task ReleaseConnection(object connection)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.ReleaseClientConnection(this, connection);
        }

        /// <summary>
        /// Reads data from a connection returned from the <see cref="AllocateConnection(IConnectivityParams)"/>
        /// method by forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connection">the connection to read from</param>
        /// <param name="data">the destination byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>a task representing the asynchronous read operation, whose result will
        /// be the number of bytes actually read. May be zero or less than the number of bytes requested.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="connection"/> or
        /// <paramref name="data"/> arguments is null</exception>
        /// <exception cref="ArgumentException">The <paramref name="offset"/> or <paramref name="length"/>
        /// arguments generate invalid offsets into <paramref name="data"/> argument.</exception>
        /// <exception cref="ArgumentException">The <paramref name="connection"/> argument is not a valid connection
        /// returned by instances of this class.</exception>
        /// <exception cref="ConnectionReleasedException">The connection has been released.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.ReadClientBytes(this, connection, data, offset, length);
        }

        /// <summary>
        /// Writes data to a connection returned from the <see cref="AllocateConnection(IConnectivityParams)"/>
        /// method by forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connection">the connection to write to</param>
        /// <param name="data">the source byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to write</param>
        /// <returns>a task representing the asynchronous write operation</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="connection"/> or
        /// <paramref name="data"/> arguments is null</exception>
        /// <exception cref="ArgumentException">The <paramref name="offset"/> or <paramref name="length"/>
        /// arguments generate invalid offsets into <paramref name="data"/> argument.</exception>
        /// <exception cref="ArgumentException">The <paramref name="connection"/> argument is not a valid connection
        /// returned by instances of this class.</exception>
        /// <exception cref="ConnectionReleasedException">The connection has been released.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.WriteClientBytes(this, connection, data, offset, length);
        }
    }
}
