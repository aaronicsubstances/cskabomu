using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// Abstract representation of the <see cref="StandardQuasiHttpClient"/> class.
    /// </summary>
    /// <remarks>
    /// This type documents the interface of the <see cref="StandardQuasiHttpClient"/> class in order to help generate
    /// implementations during testing in a statically typed language like C#.NET. Thus its prescence is not intended
    /// (in the usual sense) to suggest that the <see cref="StandardQuasiHttpClient"/> class is
    /// one of several possibilities.
    /// <para></para>
    /// For production ready needs the <see cref="StandardQuasiHttpClient"/> class is the 
    /// standard offering: any other implementation must be equivalent to it in terms of implementing the
    /// same Kabomu quasi http client protocol; else it is an incompatible implementation not having the backing
    /// of the Kabomu library.
    /// <para></para>
    /// Therefore any implementation of this interface which is not equivalent to the <see cref="StandardQuasiHttpClient"/>
    /// class, cannot be substituted for the <see cref="StandardQuasiHttpClient"/> runtime type in a variable of the static type of
    /// <see cref="IQuasiHttpClient"/>, where a production ready implementation is expected.
    /// </remarks>
    public interface IQuasiHttpClient
    {
        /// <summary>
        /// Gets or sets the default options used to send requests.
        /// </summary>
        IQuasiHttpSendOptions DefaultSendOptions { get; set; }

        /// <summary>
        /// Gets or sets the underlying transport (TCP or connection-oriented) by which connections
        /// will be allocated for sending requests and receiving responses.
        /// </summary>
        IQuasiHttpClientTransport Transport { get; set; }

        /// <summary>
        /// Gets or sets a hook point for bypassing connection-oriented transports for
        /// request processing.
        /// </summary>
        /// <remarks>
        /// This is where alternatives to connection-oriented as
        /// done by an implementation can be hooked in.
        /// </remarks>
        IQuasiHttpAltTransport TransportBypass { get; set; }

        /// <summary>
        /// Sends a quasi http request via quasi http transport
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the request to send</param>
        /// <param name="options">send options which override default send options and response
        /// streaming probability.</param>
        /// <returns>a task whose result is the reply from the remote endpoint</returns>
        Task<IQuasiHttpResponse> Send(object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions options);

        /// <summary>
        /// Sends a quasi http request via quasi http transport and makes it posssible to cancel.
        /// </summary>
        /// <param name="remoteEndpoint">the destination endpoint of the request</param>
        /// <param name="request">the request to send</param>
        /// <param name="options">send options which override default send options and response
        /// streaming probability.</param>
        /// <returns>handles which can be used to await reply from the remote endpoint, or
        /// used to cancel the request sending.</returns>
        (Task<IQuasiHttpResponse>, object) Send2(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options);

        /// <summary>
        /// Cancels an ongoing send request.
        /// </summary>
        /// <param name="sendCancellationHandle">cancellation handle received from <see cref="Send2"/></param>
        void CancelSend(object sendCancellationHandle);
    }
}
