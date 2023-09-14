using Kabomu.Abstractions;
using NLog;
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
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly IQuasiHttpProcessingOptions DefaultProcessingOptions =
            new DefaultQuasiHttpProcessingOptions();

        private readonly Stream _stream;
        private readonly ICancellableTimeoutTask _timeoutId;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string _owner; // for debugging

        public DuplexStreamConnection(Stream stream, bool isClient,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            _owner = isClient ? "client" : "server";
            Log.Debug("creating connection for {0}", _owner);

            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            ProcessingOptions = QuasiHttpUtils.MergeProcessingOptions(processingOptions,
                fallbackProcessingOptions) ?? DefaultProcessingOptions;
            _timeoutId = QuasiHttpUtils.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }
        public Task<bool> TimeoutTask => _timeoutId?.Task;
        public CancellationToken CancellationToken =>
            _cancellationTokenSource.Token;
        public IDictionary<string, object> Environment { get; set; }

        public async Task Release(bool responseStreamingEnabled)
        {
            var usageTag = responseStreamingEnabled ? "partially" : "fully";
            Log.Debug($"releasing {usageTag} for {_owner}...");
            _timeoutId?.Cancel();
            if (responseStreamingEnabled)
            {
                return;
            }
            _cancellationTokenSource.Cancel();
            await _stream.DisposeAsync();
        }

        private static string GetUsageTag(bool isResponse)
        {
            return isResponse ? "response" : "request";
        }

        public async Task Write(bool isResponse,
            byte[] encodedHeaders, Stream body)
        {
            Log.Debug($"writing {GetUsageTag(isResponse)} for {_owner}...");
            await _stream.WriteAsync(encodedHeaders, 0, encodedHeaders.Length);
            if (body != null)
            {
                await body.CopyToAsync(_stream, CancellationToken);
            }
            Log.Debug($"done writing {GetUsageTag(isResponse)} for {_owner}.");
        }

        public Task<Stream> Read(bool isResponse,
            List<byte[]> encodedHeadersReceiver)
        {
            Log.Debug($"read {GetUsageTag(isResponse)} called for {_owner}...");
            return Task.FromResult(_stream);
        }
    }
}
