﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Kabomu.Abstractions;
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

        public Stream Stream
        {
            get
            {
                // since NetworkStream demands that socket is connected before
                // creating instances of it, create on the fly.
                return new NetworkStream(_socket);
            }
        }
    }
}
