using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Kabomu.QuasiHttp.Client;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Alternative interface to <see cref="IQuasiHttpTransport"/> which provides another way for
    /// <see cref="Client.StandardQuasiHttpClient"/> instances to send quasi http requests
    /// to servers or remote endpoints.
    /// </summary>
    /// <remarks>
    /// The goal of this interface is to provide an alternative for situations in which <see cref="IQuasiHttpTransport"/>
    /// is unsuitable for sending quasi http requests. For example,
    /// <list type="bullet">
    /// <item>Memory-based transports can reduce some of the performance hit
    /// of serialization by sending requests directly to their communication endpoints,
    /// without need for allocating and releasing connections.</item>
    /// <item>Actual HTTP-based transports
    /// already have a way to send requests thanks to the myriad of HTTP client libraries out there, and so it will be
    /// unnecessary or impractical to re-invent the wheel and allocate and releasee TCP connections.</item>
    /// </list>
    /// </remarks>
    public interface IQuasiHttpAltTransport
    {
        /// <summary>
        /// Makes a direct send request on behalf of an instance of <see cref="Client.StandardQuasiHttpClient"/>.
        /// Implementations which want to support cancellation of send requests can supply a cancellation
        /// handle in the return value; otherwise they should return null cancellation handle.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the quasi http request to send.</param>
        /// <param name="sendOptions">communication endpoint information</param>
        /// <returns>a pair whose first item is a task whose result will be the quasi http response
        /// processed by this tranport instance; and whose second task is handle that can be used
        /// to attempt cancelling the send request.</returns>
        (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions);

        /// <summary>
        /// Makes a direct send request on behalf of an instance of <see cref="Client.StandardQuasiHttpClient"/>.
        /// Implementations which want to support cancellation of send requests can supply a cancellation
        /// handle in the return value; otherwise they should return null cancellation handle.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="requestFunc">a callback which receives any environment
        /// associated with how the request is to be created, and returns a promise of
        /// the request to send</param>
        /// <param name="sendOptions">communication endpoint information</param>
        /// <returns>a pair whose first item is a task whose result will be the quasi http response
        /// processed by this tranport instance; and whose second task is handle that can be used
        /// to attempt cancelling the send request.</returns>
        (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions);

        /// <summary>
        /// Attempts to cancel an ongoing send request task.
        /// </summary>
        /// <param name="sendCancellationHandle">the cancellation handle that was 
        /// returned by ProcessSendRequest() for the task to be cancelled.</param>
        void CancelSendRequest(object sendCancellationHandle);
    }
}
