﻿using Kabomu.Common;
using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Kabomu.Tests")]

namespace Kabomu.QuasiHttp
{
    public class KabomuQuasiHttpClient : IQuasiHttpClient
    {
        private readonly Dictionary<object, ITransferProtocol> _transfers;
        private readonly IParentTransferProtocol _representative;

        public KabomuQuasiHttpClient()
        {
            _transfers = new Dictionary<object, ITransferProtocol>();
            _representative = new ParentTransferProtocolImpl(this);
        }

        public int DefaultTimeoutMillis { get; set; }
        public int MaxRetryPeriodMillis { get; set; }
        public int MaxRetryCount { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void Send(object remoteEndpoint, IQuasiHttpRequestMessage request, IQuasiHttpSendOptions options,
            Action<Exception, IQuasiHttpResponseMessage> cb)
        {
            EventLoop.PostCallback(_ =>
            {
                ProcessSend(remoteEndpoint, request, options, cb);
            }, null);
        }

        private void ProcessSend(object remoteEndpoint,
            IQuasiHttpRequestMessage request,
            IQuasiHttpSendOptions options,
            Action<Exception, IQuasiHttpResponseMessage> cb)
        {
            ITransferProtocol transfer;
            if (Transport.IsByteOriented)
            {
                transfer = new ByteSendProtocol();
            }
            else
            {
                transfer = new MessageSendProtocol();
            }
            transfer.Parent = _representative;
            transfer.SendCallback = cb;
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
            }
            if (transfer.TimeoutMillis <= 0)
            {
                transfer.TimeoutMillis = DefaultTimeoutMillis;
            }
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
               _ =>
               {
                   DisableTransfer(transfer, new Exception("send timeout"));
               }, null);
            if (Transport.DirectSendRequestProcessingEnabled)
            {
                ProcessSendRequestDirectly(remoteEndpoint, transfer, request);
            }
            else
            {
                AllocateConnection(remoteEndpoint, transfer, request);
            }
        }

        private void ProcessSendRequestDirectly(object remoteEndpoint, ITransferProtocol transfer, IQuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, IQuasiHttpResponseMessage> cb = (e, res) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleDirectSendRequestProcessingOutcome(e, res, transfer);
                    }
                }, null);
            };
            Transport.ProcessSendRequest(remoteEndpoint, request, cb);
        }

        private void HandleDirectSendRequestProcessingOutcome(Exception e, IQuasiHttpResponseMessage res,
            ITransferProtocol transfer)
        {
            if (e != null)
            {
                DisableTransfer(transfer, e);
                return;
            }

            if (res == null)
            {
                DisableTransfer(transfer, new Exception("no response"));
                return;
            }

            transfer.SendCallback.Invoke(e, res);
            transfer.SendCallback = null;
            DisableTransfer(transfer, null);
        }

        private void AllocateConnection(object remoteEndpoint, ITransferProtocol transfer, IQuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, object> cb = (e, connection) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleConnectionAllocationOutcome(e, connection, transfer, request);
                    }
                }, null);
            };
            Transport.AllocateConnection(remoteEndpoint, cb);
        }

        private void HandleConnectionAllocationOutcome(Exception e, object connection, ITransferProtocol transfer,
            IQuasiHttpRequestMessage request)
        {
            if (e != null)
            {
                DisableTransfer(transfer, e);
                return;
            }

            if (connection == null)
            {
                DisableTransfer(transfer, new Exception("no connection created"));
                return;
            }

            transfer.Connection = connection;
            _transfers.Add(connection, transfer);
            transfer.OnSend(request);
        }

        public void OnReceive(object connection)
        {
            EventLoop.PostCallback(_ =>
            {
                ITransferProtocol transfer;
                if (Transport.IsByteOriented)
                {
                    transfer = new ByteReceiveProtocol();
                }
                else
                {
                    transfer = new MessageReceiveProtocol();
                }
                transfer.Parent = _representative;
                transfer.Connection = connection;
                transfer.TimeoutMillis = DefaultTimeoutMillis;
                ResetTimeout(transfer);
                _transfers.Add(connection, transfer);
                transfer.OnReceive();
            }, null);
        }

        public void OnReceiveMessage(object connection, byte[] data, int offset, int length)
        {
            EventLoop.PostCallback(_ =>
            {
                if (!_transfers.ContainsKey(connection))
                {
                    return;
                }
                var transfer = _transfers[connection];
                transfer.OnReceiveMessage(data, offset, length);
            }, null);
        }

        public void Reset(Exception cause, Action<Exception> cb)
        {
            EventLoop.PostCallback(_ =>
            {
                try
                {
                    ProcessReset(cause ?? new Exception("reset"));
                    cb?.Invoke(null);
                }
                catch (Exception e)
                {
                    cb?.Invoke(e);
                }
            }, null);
        }

        private void ResetTimeout(ITransferProtocol transfer)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("send timeout"));
                }, null);
        }

        private void AbortTransfer(ITransferProtocol transfer, Exception e)
        {
            if (!_transfers.Remove(transfer.Connection))
            {
                return;
            }
            DisableTransfer(transfer, e);
        }

        private void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _transfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _transfers.Clear();
        }

        private void DisableTransfer(ITransferProtocol transfer, Exception e)
        {
            transfer.Cancel(e);
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.ProcessingCancellationIndicator?.Cancel();
            transfer.SendCallback?.Invoke(e, null);
            transfer.SendCallback = null;

            if (transfer.Connection != null)
            {
                Transport.ReleaseConnection(transfer.Connection);
            }

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "transfer error");
            }
        }

        private class ParentTransferProtocolImpl : IParentTransferProtocol
        {
            private readonly KabomuQuasiHttpClient _delegate;

            public ParentTransferProtocolImpl(KabomuQuasiHttpClient passThrough)
            {
                _delegate = passThrough;
            }

            public int DefaultTimeoutMillis => _delegate.DefaultTimeoutMillis;

            public int MaxRetryPeriodMillis => _delegate.MaxRetryPeriodMillis;

            public int MaxRetryCount => _delegate.MaxRetryCount;

            public IQuasiHttpApplication Application => _delegate.Application;

            public IQuasiHttpTransport Transport => _delegate.Transport;

            public IEventLoopApi EventLoop => _delegate.EventLoop;

            public UncaughtErrorCallback ErrorHandler => _delegate.ErrorHandler;

            public void AbortTransfer(ITransferProtocol transfer, Exception e)
            {
                _delegate.AbortTransfer(transfer, e);
            }
        }
    }
}