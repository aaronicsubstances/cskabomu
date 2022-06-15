using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Kabomu.Tests")]

namespace Kabomu.QuasiHttp
{
    public class KabomuQuasiHttpClient : IQuasiHttpClient
    {
        private readonly Dictionary<object, ITransferProtocolInternal> _transfersWithConnections;
        private readonly HashSet<ITransferProtocolInternal> _transfersWithoutConnections;
        private readonly IParentTransferProtocolInternal _representative;

        public KabomuQuasiHttpClient()
        {
            _transfersWithConnections = new Dictionary<object, ITransferProtocolInternal>();
            _transfersWithoutConnections = new HashSet<ITransferProtocolInternal>();
            _representative = new ParentTransferProtocolImpl(this);
        }

        public int DefaultTimeoutMillis { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void Send(object remoteEndpoint, IQuasiHttpRequest request, IQuasiHttpSendOptions options,
            Action<Exception, IQuasiHttpResponse> cb)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
            }
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            EventLoop.RunExclusively(_ =>
            {
                ProcessSend(remoteEndpoint, request, options, cb);
            }, null);
        }

        private void ProcessSend(object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions options,
            Action<Exception, IQuasiHttpResponse> cb)
        {
            var transfer = new SendProtocolInternal
            {
                Parent = _representative,
                SendCallback = cb
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
            }
            if (transfer.TimeoutMillis <= 0)
            {
                transfer.TimeoutMillis = DefaultTimeoutMillis;
            }
            _transfersWithoutConnections.Add(transfer);
            ResetTimeout(transfer, true);
            if (Transport.DirectSendRequestProcessingEnabled)
            {
                ProcessSendRequestDirectly(remoteEndpoint, transfer, request);
            }
            else
            {
                AllocateConnection(remoteEndpoint, transfer, request);
            }
        }

        private void ProcessSendRequestDirectly(object remoteEndpoint, ITransferProtocolInternal transfer, IQuasiHttpRequest request)
        {
            var cancellationIndicator = new STCancellationIndicatorInternal();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, IQuasiHttpResponse> cb = (e, res) =>
            {
                EventLoop.RunExclusively(_ =>
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

        private void HandleDirectSendRequestProcessingOutcome(Exception e, IQuasiHttpResponse res,
            ITransferProtocolInternal transfer)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            if (res == null)
            {
                AbortTransfer(transfer, new Exception("no response"));
                return;
            }

            transfer.SendCallback.Invoke(e, res);
            transfer.SendCallback = null;
            AbortTransfer(transfer, null);
        }

        private void AllocateConnection(object remoteEndpoint, ITransferProtocolInternal transfer, IQuasiHttpRequest request)
        {
            var cancellationIndicator = new STCancellationIndicatorInternal();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, object> cb = (e, connection) =>
            {
                EventLoop.RunExclusively(_ =>
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

        private void HandleConnectionAllocationOutcome(Exception e, object connection, ITransferProtocolInternal transfer,
            IQuasiHttpRequest request)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            if (connection == null)
            {
                AbortTransfer(transfer, new Exception("no connection created"));
                return;
            }

            transfer.Connection = connection;
            _transfersWithConnections.Add(connection, transfer);
            _transfersWithoutConnections.Remove(transfer);
            transfer.OnSend(request);
        }

        public void OnReceive(object connection)
        {
            if (connection == null)
            {
                throw new ArgumentException("null connection");
            }
            EventLoop.RunExclusively(_ =>
            {
                var transfer = new ReceiveProtocolInternal
                {
                    Parent = _representative,
                    Connection = connection,
                    TimeoutMillis = DefaultTimeoutMillis
                };
                _transfersWithConnections.Add(connection, transfer);
                ResetTimeout(transfer, false);
                transfer.OnReceive();
            }, null);
        }

        public void Reset(Exception cause, Action<Exception> cb)
        {
            EventLoop.RunExclusively(_ =>
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

        private void ResetTimeout(ITransferProtocolInternal transfer, bool forSend)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception((forSend ? "send" : "receive") + " timeout"));
                }, null);
        }

        private void AbortTransfer(ITransferProtocolInternal transfer, Exception e)
        {
            if (transfer.Connection != null && _transfersWithConnections.Remove(transfer.Connection))
            {
                DisableTransfer(transfer, e);
                return;
            }
            if (_transfersWithoutConnections.Remove(transfer))
            {
                DisableTransfer(transfer, e);
                return;
            }
        }

        private void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _transfersWithConnections.Values)
            {
                try
                {
                    DisableTransfer(transfer, causeOfReset);
                }
                catch (Exception) { }
            }
            foreach (var transfer in _transfersWithoutConnections)
            {
                try
                {
                    DisableTransfer(transfer, causeOfReset);
                }
                catch (Exception) { }
            }
            _transfersWithConnections.Clear();
            _transfersWithoutConnections.Clear();
        }

        private void DisableTransfer(ITransferProtocolInternal transfer, Exception e)
        {
            transfer.Cancel(e);
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.ProcessingCancellationIndicator?.Cancel();

            try
            {
                if (e != null)
                {
                    ErrorHandler?.Invoke(e, "transfer error");
                }

                if (transfer.Connection != null)
                {
                    Transport.OnReleaseConnection(transfer.Connection);
                }
            }
            finally
            {
                transfer.SendCallback?.Invoke(e, null);
                transfer.SendCallback = null;
            }
        }

        private class ParentTransferProtocolImpl : IParentTransferProtocolInternal
        {
            private readonly KabomuQuasiHttpClient _delegate;

            public ParentTransferProtocolImpl(KabomuQuasiHttpClient passThrough)
            {
                _delegate = passThrough;
            }

            public int DefaultTimeoutMillis => _delegate.DefaultTimeoutMillis;

            public IQuasiHttpApplication Application => _delegate.Application;

            public IQuasiHttpTransport Transport => _delegate.Transport;

            public IMutexApi Mutex => _delegate.EventLoop;

            public UncaughtErrorCallback ErrorHandler => _delegate.ErrorHandler;

            public void AbortTransfer(ITransferProtocolInternal transfer, Exception e)
            {
                _delegate.AbortTransfer(transfer, e);
            }
        }
    }
}
