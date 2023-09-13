using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Abstractions;
using Kabomu.ProtocolImpl;
using System.IO;
using System.Threading;
using NLog;

namespace Kabomu.Examples.Shared
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpConnection"/> interface
    /// based on .NET sockets.
    /// </summary>
    public class SocketConnection : IQuasiHttpConnection
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly IQuasiHttpProcessingOptions DefaultProcessingOptions =
            new DefaultQuasiHttpProcessingOptions();

        private readonly Socket _socket;
        private readonly Stream _inputStream;
        private readonly ICancellableTimeoutTask _timeoutId;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string _owner; // for debugging

        public SocketConnection(Socket socket, bool isClient,
            IQuasiHttpProcessingOptions processingOptions,
            IQuasiHttpProcessingOptions fallbackProcessingOptions = null)
        {
            _owner = isClient ? "client" : "server";
            Log.Debug("creating connection for {0}", _owner);

            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _inputStream = new SocketBackedStream(socket);
            ProcessingOptions = MiscUtils.MergeProcessingOptions(processingOptions,
                fallbackProcessingOptions) ?? DefaultProcessingOptions;
            _timeoutId = MiscUtils.CreateCancellableTimeoutTask(
                ProcessingOptions.TimeoutMillis);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public IQuasiHttpProcessingOptions ProcessingOptions { get; }
        public Task<bool> TimeoutTask => _timeoutId?.Task;
        public CancellationToken CancellationToken =>
            _cancellationTokenSource.Token;
        public IDictionary<string, object> Environment { get; set; }

        private async Task WriteSocketBytes(byte[] data, int offset, int length)
        {
            int totalBytesSent = 0;
            while (totalBytesSent < length)
            {
                int bytesSent = await _socket.SendAsync(
                    new ReadOnlyMemory<byte>(data,
                        offset + totalBytesSent,
                        length - totalBytesSent), SocketFlags.None);
                totalBytesSent += bytesSent;
            }
        }

        public Task Release(bool responseStreamingEnabled)
        {
            var usageTag = responseStreamingEnabled ? "partially" : "fully";
            Log.Debug($"releasing {usageTag} for {_owner}...");
            _timeoutId?.Cancel();
            if (responseStreamingEnabled)
            {
                return Task.CompletedTask;
            }
            _cancellationTokenSource.Cancel();
            _socket.Dispose();
            return Task.CompletedTask;
        }

        private static string GetUsageTag(bool isResponse)
        {
            return isResponse ? "response" : "request";
        }

        public async Task Write(bool isResponse, byte[] encodedHeaders,
            Stream body)
        {
            Log.Debug($"writing {GetUsageTag(isResponse)} for {_owner}...");
            await WriteSocketBytes(encodedHeaders, 0, encodedHeaders.Length);
            if (body != null)
            {
                await MiscUtils.CopyBytesToSink(body, WriteSocketBytes, -1,
                    CancellationToken);
            }
            Log.Debug($"done writing {GetUsageTag(isResponse)} for {_owner}.");
        }

        public async Task<Stream> Read(bool isResponse,
            List<byte[]> encodedHeadersReceiver)
        {
            Log.Debug($"reading {GetUsageTag(isResponse)} for {_owner}...");
            await QuasiHttpCodec.ReadEncodedHeaders(_inputStream,
                encodedHeadersReceiver, ProcessingOptions.MaxHeadersSize,
                CancellationToken);
            Log.Debug($"done reading {GetUsageTag(isResponse)} for {_owner}.");
            return _inputStream;
        }
    }
}
