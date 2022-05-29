using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Kabomu.Examples.Shared
{
    public class LocalhostUdpTransport : IQuasiHttpTransport
    {
        private readonly UdpClient _udpClient;
        private readonly Dictionary<LocalhostUdpConnection, LocalhostUdpConnection> _unackedConnections;

        public LocalhostUdpTransport(int port)
        {
            _udpClient = new UdpClient(port);
            _unackedConnections = new Dictionary<LocalhostUdpConnection, LocalhostUdpConnection>();
            ConnectionRetryIntervalMilis = 1000;
            MaxConnectionRetryCount = 3;
        }

        public bool IsByteOriented => false;

        public int MaxMessageOrChunkSize => 65_000;

        public bool DirectSendRequestProcessingEnabled => false;

        public int ConnectionRetryIntervalMilis { get; set; }

        public int MaxConnectionRetryCount { get; set; }

        public KabomuQuasiHttpClient Upstream { get; set; }

        public IEventLoopApi EventLoop { get; set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequestMessage request,
            Action<Exception, IQuasiHttpResponseMessage> cb)
        {
            throw new NotImplementedException();
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var port = (int)remoteEndpoint;
            if (cb == null)
            {
                throw new Exception("null callback");
            }
            var connection = new LocalhostUdpConnection
            {
                PeerPort = port,
                ConnectionId = Guid.NewGuid().ToString("n"),
                AllocateConnectionCallback = cb,
                ConnectionRetryCount = 0
            };
            _unackedConnections.Add(connection, connection);
            SendConnectionPdu(connection);
            if (MaxConnectionRetryCount > 0)
            {
                ResetReconnectInterval(connection);
            }
        }

        private void SendConnectionPdu(LocalhostUdpConnection connection)
        {
            var synPdu = new LocalhostUdpDatagram
            {
                ConnectionId = connection.ConnectionId,
                Version = LocalhostUdpDatagram.Version01,
                PduType = LocalhostUdpDatagram.PduTypeSyn
            };
            SendMessage(connection, synPdu, e =>
            {
                if (e != null)
                {
                    connection.AllocateConnectionCallback.Invoke(e, null);
                }
            });
        }

        private void ResetReconnectInterval(LocalhostUdpConnection connection)
        {
            EventLoop.CancelTimeout(connection.ConnectionRetryBackoffId);
            connection.ConnectionRetryBackoffId = EventLoop.ScheduleTimeout(ConnectionRetryIntervalMilis,
                OnReconnectInterval, connection);
        }

        private void OnReconnectInterval(object obj)
        {
            var connection = (LocalhostUdpConnection)obj;
            connection.ConnectionRetryCount++;
            if (connection.ConnectionRetryCount < MaxConnectionRetryCount)
            {
                SendConnectionPdu(connection);
                ResetReconnectInterval(connection);
            }
        }

        private void TryEstablishingConnection(LocalhostUdpConnection connection)
        {
            if (!_unackedConnections.ContainsKey(connection))
            {
                // ignore pdus from previous connections which no longer exist.
                return;
            }
            connection = _unackedConnections[connection];
            connection.AllocateConnectionCallback.Invoke(null, connection);
            connection.AllocateConnectionCallback = null;
            EjectConnection(connection, null);
        }

        public void ReleaseConnection(object obj)
        {
            var connection = (LocalhostUdpConnection)obj;
            EjectConnection(connection, null);
        }

        private void EjectConnection(LocalhostUdpConnection connection, Exception e)
        {
            EventLoop.CancelTimeout(connection.ConnectionRetryBackoffId);
            _unackedConnections.Remove(connection);
            connection.AllocateConnectionCallback?.Invoke(e ?? new Exception("connect error"), null);
            connection.AllocateConnectionCallback = null;
        }

        public async void Start()
        {
            while (true)
            {
                try
                {
                    var datagram = await _udpClient.ReceiveAsync();
                    var pdu = LocalhostUdpDatagram.Deserialize(datagram.Buffer, 0, datagram.Buffer.Length);
                    EventLoop.PostCallback(_ => ProcessReceivedMessage(datagram.RemoteEndPoint.Port, pdu), null);
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
        }

        public void Stop()
        {
            _udpClient.Dispose();
        }

        private void ProcessReceivedMessage(int port, LocalhostUdpDatagram pdu)
        {
            if (pdu.Version != LocalhostUdpDatagram.Version01)
            {
                throw new Exception("unknown pdu version: " + pdu.Version);
            }

            var connection = new LocalhostUdpConnection
            {
                PeerPort = port,
                ConnectionId = pdu.ConnectionId,
            };

            if (pdu.PduType == LocalhostUdpDatagram.PduTypeSyn)
            {
                // let upstream application deal with duplicate connection attempts.
                Upstream.OnReceive(connection);

                // send a syn ack to establish connection at initiating end.
                var connectionConfirmationPdu = new LocalhostUdpDatagram
                {
                    Version = LocalhostUdpDatagram.Version01,
                    PduType = LocalhostUdpDatagram.PduTypeSynAck,
                    ConnectionId = pdu.ConnectionId
                };
                SendMessage(connection, connectionConfirmationPdu, null);
            }
            else if (pdu.PduType == LocalhostUdpDatagram.PduTypeSynAck)
            {
                TryEstablishingConnection(connection);
            }
            else if (pdu.PduType == LocalhostUdpDatagram.PduTypeData)
            {
                // let upstream application deal with possible duplication.
                Upstream.OnReceiveMessage(connection, pdu.Data, pdu.DataOffset, pdu.DataLength);
            }
            else
            {
                throw new Exception("unknown pdu type: " + pdu.PduType);
            }
        }

        public void SendMessage(object connection, byte[] data, int offset, int length,
            Action<Action<bool>> cancellationEnquirer, Action<Exception> cb)
        {
            var typedConnection = (LocalhostUdpConnection)connection;
            var pdu = new LocalhostUdpDatagram
            {
                Version = LocalhostUdpDatagram.Version01,
                PduType = LocalhostUdpDatagram.PduTypeData,
                ConnectionId = typedConnection.ConnectionId,
                Data = data,
                DataOffset = offset,
                DataLength = length
            };
            SendMessage(typedConnection, pdu, cb);
        }

        private async void SendMessage(LocalhostUdpConnection connection, LocalhostUdpDatagram pdu, Action<Exception> cb)
        {
            var pduBytes = pdu.Serialize();
            try
            {
                int sent = await _udpClient.SendAsync(pduBytes, pduBytes.Length, "localhost", connection.PeerPort);
                if (sent != pduBytes.Length)
                {
                    throw new Exception("could not send all bytes in datagram");
                }
                else
                {
                    // NB: Can be sent even if peer port is not bound.
                }
                cb?.Invoke(null);
            }
            catch (Exception e)
            {
                cb?.Invoke(e);
            }
        }
    }
}
