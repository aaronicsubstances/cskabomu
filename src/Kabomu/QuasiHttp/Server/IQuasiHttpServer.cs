using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    /// <summary>
    /// Abstract representation of the <see cref="StandardQuasiHttpServer"/> class.
    /// </summary>
    /// <remarks>
    /// Usually an implementing class exists as one of several possibilities of realizing an interface. But not
    /// in this case: this type exists to mirror the interface of the <see cref="DefaultQuasiHttpResponse"/> class
    /// in order to help generate implementations during testing in a statically typed language like C#.NET.
    /// <para></para>
    /// For a production ready implementation the <see cref="StandardQuasiHttpServer"/> class is the 
    /// standard offering: any other implementation must be equivalent to it in terms of implementing the
    /// same Kabomu quasi http server protocol; else it is an incompatible implementation not having the backing
    /// of the Kabomu library.
    /// <para></para>
    /// Therefore any implementation of this interface which is not equivalent to the <see cref="StandardQuasiHttpServer"/>
    /// class, cannot be substituted for the <see cref="StandardQuasiHttpServer"/> runtime type in a variable of the static type of
    /// <see cref="IQuasiHttpServer"/>, where a production ready implementation is expected.
    /// </remarks>
    public interface IQuasiHttpServer
    {
        /// <summary>
        /// Gets or sets the default options used to process receive requests.
        /// </summary>
        IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }

        /// <summary>
        /// Gets or sets a callback which can be used to report errors of processing requests
        /// received from connections.
        /// </summary>
        UncaughtErrorCallback ErrorHandler { get; set; }

        /// <summary>
        /// Gets or sets the quasi web application responsible for processing requests to generate
        /// responses.
        /// </summary>
        IQuasiHttpApplication Application { get; set; }


        /// <summary>
        /// Gets or sets the underlying transport (TCP or connection-oriented) for retrieving requests
        /// for quasi web applications, and for sending responses generated from quasi web applications.
        /// </summary>
        IQuasiHttpServerTransport Transport { get; set; }

        /// <summary>
        /// Gets and sets a mutex object which will most likely be needed to synchronize server operations.
        /// <para>
        /// This property is exposed publicly to allow frameworks employing a general concurrency mechanism
        /// to impose their policy through this property.
        /// </para>
        /// </summary>
        IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Gets and sets a timer object which will most likely be needed to impose timeouts on server operations.
        /// <para>
        /// This property is exposed publicly to allow frameworks employing a general concurrency mechanism
        /// to impose their policy through this property.
        /// </para>
        /// </summary>
        ITimerApi TimerApi { get; set; }

        /// <summary>
        /// Starting point of implementations for receiving connections from their quasi http transports.
        /// </summary>
        /// <returns>a task representing asynchronous operation</returns>
        Task Start();

        /// <summary>
        /// Used by implementations to stop receiving connections from their quasi http transports, and 
        /// optionally releasing any ongoing connections.
        /// </summary>
        /// <param name="resetTimeMillis">if nonnegative, then it indicates the delay in ms
        /// from the current time after which all ongoing connections will be forcefully released.</param>
        /// <returns>a task representing asynchronous operations.</returns>
        Task Stop(int resetTimeMillis);

        /// <summary>
        /// Releases all ongoing connections. Implementations must allow this to be called
        /// regardless of whether instance is running.
        /// </summary>
        /// <param name="cause">optional exception object indicating cause of reset.</param>
        /// <returns>a task representing asynchronous operation.</returns>
        Task Reset(Exception cause);

        /// <summary>
        /// Hook for transports external to Kabomu for using the quasi http application of an
        /// implementation to process quasi http requests, and leverage the timeout feature of 
        /// the implementation. Thus this call provides a better option than calling on the
        /// quasi http application directly.
        /// </summary>
        /// <param name="request">quasi http request to process </param>
        /// <param name="options">any processing options which should override the default processing options</param>
        /// <returns>a task whose result will be the response generated by the quasi http application</returns>
        Task<IQuasiHttpResponse> ProcessReceiveRequest(IQuasiHttpRequest request,
            IQuasiHttpProcessingOptions options);
    }
}
