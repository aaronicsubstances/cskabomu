using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Abstractions;
using System.IO;

namespace Kabomu.Examples.Shared
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpConnection"/> interface
    /// based on .NET sockets.
    /// </summary>
    public class SocketConnection : IQuasiHttpConnection
    {
        private static readonly IQuasiHttpProcessingOptions DefaultProcessingOptions =
            new DefaultQuasiHttpProcessingOptions();

        private readonly ICancellableTimeoutTask _timeoutId;

        public SocketConnection(Socket socket, object clientPortOrPath,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            ClientPortOrPath = clientPortOrPath;
            ProcessingOptions = QuasiHttpUtils.MergeProcessingOptions(processingOptions,
                fallbackProcessingOptions) ?? DefaultProcessingOptions;
            _timeoutId = QuasiHttpUtils.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis);
        }

        internal Socket Socket { get; }
        internal object ClientPortOrPath { get; }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }
        public Task<bool> TimeoutTask => _timeoutId?.Task;
        public CustomTimeoutScheduler TimeoutScheduler => null;
        public IDictionary<string, object> Environment { get; set; }

        public Task Release(IQuasiHttpResponse response)
        {
            _timeoutId?.Cancel();
            if (response?.Body != null)
            {
                return Task.CompletedTask;
            }
            Socket.Dispose();
            return Task.CompletedTask;
        }

        public Stream Stream
        {
            get
            {
                // since NetworkStream demands that socket is connected before
                // creating instances of it, create on the fly.
                return new NetworkStream(Socket);
            }
        }
    }
}
