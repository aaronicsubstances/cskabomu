using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represens objects needed by
    /// <see cref="IQuasiHttpTransport"/> instances for reading or writing
    /// data.
    /// </summary>
    public interface IQuasiHttpConnection
    {
        /// <summary>
        /// Gets the effective processing options that will be used to
        /// configure and perform response buffering, and any other
        /// operations by <see cref="StandardQuasiHttpClient"/>
        /// and <see cref="StandardQuasiHttpServer"/> instances.
        /// </summary>
        IQuasiHttpProcessingOptions ProcessingOptions { get; }

        /// <summary>
        /// Gets an optional task which can be used
        /// by <see cref="StandardQuasiHttpClient"/>
        /// and <see cref="StandardQuasiHttpServer"/> instances,
        /// to impose timeouts on request processing
        /// if and only if it returns true.
        /// </summary>
        Task<bool> TimeoutTask { get; }

        /// <summary>
        /// Gets an optional function which will be used instead of
        /// <see cref="TimeoutTask"/> in <see cref="StandardQuasiHttpClient"/>
        /// and <see cref="StandardQuasiHttpServer"/> instances, to impose
        /// timeouts on request processing.
        /// </summary>
        CustomTimeoutScheduler TimeoutScheduler { get; }

        /// <summary>
        /// Gets any environment variables that can control decisions
        /// during operations by <see cref="StandardQuasiHttpClient"/> and
        /// <see cref="StandardQuasiHttpServer"/> instances.
        /// </summary>
        IDictionary<string, object> Environment { get; }
    }
}
