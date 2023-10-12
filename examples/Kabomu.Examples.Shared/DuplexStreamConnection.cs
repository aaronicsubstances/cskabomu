using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpConnection"/> interface
    /// based on .NET Streams.
    /// </summary>
    public class DuplexStreamConnection : IQuasiHttpConnection
    {
        private static readonly IQuasiHttpProcessingOptions DefaultProcessingOptions =
            new DefaultQuasiHttpProcessingOptions();

        private readonly ICancellableTimeoutTask _timeoutId;

        public DuplexStreamConnection(Stream stream, bool isClient,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            ProcessingOptions = QuasiHttpUtils.MergeProcessingOptions(processingOptions,
                fallbackProcessingOptions) ?? DefaultProcessingOptions;
            _timeoutId = QuasiHttpUtils.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis);
        }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }
        public Task<bool> TimeoutTask => _timeoutId?.Task;
        public CustomTimeoutScheduler TimeoutScheduler => null;
        public IDictionary<string, object> Environment { get; set; }
        public Stream Stream { get; }

        public async Task Release(IQuasiHttpResponse response)
        {
            _timeoutId?.Cancel();
            if (response?.Body != null)
            {
                return;
            }
            await Stream.DisposeAsync();
        }
    }
}
