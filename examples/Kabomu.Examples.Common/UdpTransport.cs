using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Kabomu.Examples.Common
{
    public class UdpTransport : IQuasiHttpTransport
    {
        private readonly UdpClient _udpClient;
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<int, QpcTransaction> _transactions;
        private readonly Random _randGen = new Random();

        public UdpTransport(int port, CancellationToken cancellationToken)
        {
            _udpClient = new UdpClient(port);
            _cancellationToken = cancellationToken;
            _transactions = new Dictionary<int, QpcTransaction>();
        }

        public IQuasiHttpClient Upstream { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public int MaximumChunkSize => 65_000;
        public bool DirectSendRequestProcessingEnabled => false;
        public bool IsChunkDeliveryAcknowledged => false;
        public int MinRetryBackoffMillis { get; set; }
        public int MaxRetryBackoffMillis { get; set; }
        public int MaxRetryCount { get; set; }
        public int TimeWaitMillis { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, QuasiHttpRequestMessage request,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            throw new NotImplementedException();
        }

        public void Write(object connection, byte[] data, int offset, int length, 
            Action<Exception> cb)
        {
            var transaction = (QpcTransaction)connection;
            if (transaction.Data != null)
            {
                throw new InvalidOperationException();
            }
            transaction.Data = data;
            transaction.DataOffset = offset;
            transaction.DataLength = length;
            transaction.RequestCallback = cb;
            RetrySendPdu(transaction);
        }

        public void Read(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            throw new NotImplementedException();
        }

        public async void Start()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var datagram = await _udpClient.ReceiveAsync();
                    var pdu = UdpTransportDatagram.Deserialize(datagram.Buffer, 0, datagram.Buffer.Length);
                    EventLoop.PostCallback(_ => ProcessReceivedPdu(datagram.RemoteEndPoint, pdu), null);
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException)
                    {
                        break;
                    }
                    else
                    {
                        ErrorHandler?.Invoke(e, "error encountered during receiving");
                    }
                }
            }
            _udpClient.Dispose();
        }

        private void ProcessReceivedPdu(IPEndPoint remoteEndPoint, UdpTransportDatagram pdu)
        {
            QpcTransaction transaction;
            if (pdu.PduType == UdpTransportDatagram.PduTypeRequest)
            {
                if (_transactions.ContainsKey(pdu.RequestId))
                {
                    RetrySendPdu(_transactions[pdu.RequestId]);
                    return;
                }
                transaction = new QpcTransaction
                {
                    RemoteEndpoint = remoteEndPoint,
                    RequestId = pdu.RequestId,
                    PduType = UdpTransportDatagram.PduTypeResponse,
                };
                _transactions.Add(pdu.RequestId, transaction);
            }
            else if (pdu.PduType == UdpTransportDatagram.PduTypeResponse)
            {
                transaction = _transactions[pdu.RequestId];
            }
            else
            {
                throw new Exception("unknown UDP transport pdu type: " + pdu.PduType);
            }
            Upstream.OnReceive(transaction, pdu.Data, pdu.DataOffset, pdu.DataLength);
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var transaction = new QpcTransaction
            {
                PduType = UdpTransportDatagram.PduTypeRequest
            };
            if (remoteEndpoint is int)
            {
                transaction.RemoteEndpoint = new IPEndPoint(IPAddress.Loopback, (int)remoteEndpoint);
            }
            else
            {
                transaction.RemoteEndpoint = (IPEndPoint)remoteEndpoint;
            }
            transaction.RequestId = _randGen.Next();
            _transactions.Add(transaction.RequestId, transaction);
            cb.Invoke(null, transaction);
        }

        public void ReleaseConnection(object connection)
        {
            var transaction = (QpcTransaction)connection;
            if (transaction.Data == null)
            {
                AbortTransaction(transaction, null);
                return;
            }
            transaction.TimeWaitId = EventLoop.ScheduleTimeout(TimeWaitMillis, _ =>
            {
                AbortTransaction(transaction, null);
            }, null);
        }

        private void RetrySendPdu(QpcTransaction transaction)
        {
            var pdu = new UdpTransportDatagram
            {
                Version = UdpTransportDatagram.Version01,
                PduType = transaction.PduType,
                RequestId = transaction.RequestId,
                Data = transaction.Data,
                DataOffset = transaction.DataOffset,
                DataLength = transaction.DataLength
            };
            var datagram = pdu.Serialize();
            try
            {
                _udpClient.BeginSend(datagram, datagram.Length, transaction.RemoteEndpoint,
                    new AsyncCallback(HandleSendPduOutcome), transaction);
            }
            catch (Exception e)
            {
                AbortTransaction(transaction, e);
            }
        }

        private void HandleSendPduOutcome(IAsyncResult ar)
        {
            var transaction = (QpcTransaction)ar.AsyncState;
            EventLoop.PostCallback(_ =>
            {
                if (!_transactions.ContainsKey(transaction.RequestId))
                {
                    return;
                }
                try
                {
                    int sent = _udpClient.EndSend(ar);
                    // NB: Can be sent even if target port is not bound.
                    if (sent != transaction.DataLength)
                    {
                        throw new Exception("sent less bytes");
                    }
                    transaction.RequestCallback?.Invoke(null);
                    transaction.RequestCallback = null;
                    if (transaction.PduType == UdpTransportDatagram.PduTypeRequest)
                    {
                        int backoffMillis = _randGen.Next(MinRetryBackoffMillis,
                            MaxRetryBackoffMillis + 1);
                        transaction.RetryBackoffTimeoutId = EventLoop.ScheduleTimeout(backoffMillis,
                            _ => HandleRetryBackoffTimeout(transaction), null);
                    }
                }
                catch (Exception e)
                {
                    AbortTransaction(transaction, e);
                }
            }, null);
        }

        private void HandleRetryBackoffTimeout(QpcTransaction transaction)
        {
            if (transaction.RetryCount < MaxRetryCount)
            {
                transaction.RetryCount++;
                RetrySendPdu(transaction);
            }
        }

        private void AbortTransaction(QpcTransaction transaction, Exception e)
        {
            if (!_transactions.Remove(transaction.RequestId))
            {
                return;
            }
            EventLoop.CancelTimeout(transaction.RetryBackoffTimeoutId);
            EventLoop.CancelTimeout(transaction.TimeWaitId);
            transaction.RequestCallback?.Invoke(e);
        }
    }
}
