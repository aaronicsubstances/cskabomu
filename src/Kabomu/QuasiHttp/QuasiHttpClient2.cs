﻿using Kabomu.Common;
using Kabomu.QuasiHttp.Internals;
using Kabomu.QuasiHttp.Internals.ByteOrientedProtocols;
using Kabomu.QuasiHttp.Internals.MessageOrientedProtocols;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpClient2 : IQuasiHttpClient
    {
        private readonly Dictionary<object, ITransferProtocol> _transfers;
        private readonly IParentTransferProtocol _representative;

        public QuasiHttpClient2()
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

        public void Send(object remoteEndpoint, QuasiHttpRequestMessage request, QuasiHttpSendOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            EventLoop.PostCallback(_ =>
            {
                ProcessSend(remoteEndpoint, request, options, cb);
            }, null);
        }

        private void ProcessSend(object remoteEndpoint,
            QuasiHttpRequestMessage request,
            QuasiHttpSendOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            ITransferProtocol transfer;
            if (Transport.IsByteOriented)
            {
                transfer = new ByteSendProtocol2();
            }
            else
            {
                transfer = new MessageSendProtocol2();
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

        private void ProcessSendRequestDirectly(object remoteEndpoint, ITransferProtocol transfer, QuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, QuasiHttpResponseMessage> cb = (e, res) =>
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

        private void HandleDirectSendRequestProcessingOutcome(Exception e, QuasiHttpResponseMessage res,
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

        private void AllocateConnection(object remoteEndpoint, ITransferProtocol transfer, QuasiHttpRequestMessage request)
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
            QuasiHttpRequestMessage request)
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
                    transfer = new ByteReceiveProtocol2();
                }
                else
                {
                    transfer = new MessageReceiveProtocol2();
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
            private readonly QuasiHttpClient2 _delegate;

            public ParentTransferProtocolImpl(QuasiHttpClient2 passThrough)
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
