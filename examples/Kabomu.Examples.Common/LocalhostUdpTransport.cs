using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Kabomu.Examples.Common
{
    public class LocalhostUdpTransport : IQuasiHttpTransport
    {
        private readonly UdpClient _udpClient;
        private readonly Dictionary<int, Dictionary<string, LocalhostUdpConnection>> _connections;
        private readonly Random _randGen = new Random();

        public LocalhostUdpTransport(int port)
        {
            _udpClient = new UdpClient(port);
            _connections = new Dictionary<int, Dictionary<string, LocalhostUdpConnection>>();
            ConnectTimeoutMilis = 1000;
            ReadTimeoutMilis = 1000;
            MaxConnectionRetryCount = 3;
        }

        public bool IsByteOriented => false;

        public int MaxMessageSize => 65_000;

        public bool DirectSendRequestProcessingEnabled => false;

        public int MaxConnectionRetryCount { get; set; }

        public int ConnectTimeoutMilis { get; set; }

        public int ReadTimeoutMilis { get; set; }

        public IQuasiHttpClient Upstream { get; set; }

        public IEventLoopApi EventLoop { get; set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            throw new NotImplementedException();
        }

        public void ProcessSendRequest(object remoteEndpoint, QuasiHttpRequestMessage request,
            Action<Exception, QuasiHttpResponseMessage> cb)
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
                ConnectionId = Guid.NewGuid().ToString("n"),
                DestinationPort = port,
                AllocateConnectionCallback = cb,
                ConnectionRetryCount = 0
            };
            if (!_connections.ContainsKey(port))
            {
                _connections.Add(port, new Dictionary<string, LocalhostUdpConnection>());
            }
            _connections[port].Add(connection.ConnectionId, connection);
            connection.ConnectionTimeoutId = EventLoop.ScheduleTimeout(ConnectTimeoutMilis, obj =>
            {
                AbortConnection(connection, new Exception("connect timeout"));
            }, null);
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
            var reconnectBackoffRange = MiscUtils.CalculateRetryBackoffRange(ConnectTimeoutMilis,
                MaxConnectionRetryCount - connection.ConnectionRetryCount);
            var reconnectDelay = _randGen.Next(reconnectBackoffRange[0], reconnectBackoffRange[1]);
            EventLoop.CancelTimeout(connection.ConnectionRetryBackoffId);
            connection.ConnectionRetryBackoffId = EventLoop.ScheduleTimeout(reconnectDelay,
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

        private void EstablishConnection(LocalhostUdpConnection connection)
        {
            if (connection.Established)
            {
                return;
            }
            connection.Established = true;
            connection.AllocateConnectionCallback.Invoke(null, connection);
            connection.AllocateConnectionCallback = null;
            EventLoop.CancelTimeout(connection.ConnectionRetryBackoffId);
            ResetReadTimeout(connection);
            EventLoop.CancelTimeout(connection.ConnectionTimeoutId);
        }

        public void ReleaseConnection(object obj)
        {
            var connection = (LocalhostUdpConnection)obj;
            AbortConnection(connection, null);
            var finPdu = new LocalhostUdpDatagram
            {
                Version = LocalhostUdpDatagram.Version01,
                ConnectionId = connection.ConnectionId,
                PduType = LocalhostUdpDatagram.PduTypeFin
            };
            SendMessage(connection, finPdu, null);
        }

        private void AbortConnection(LocalhostUdpConnection connection, Exception e)
        {
            EventLoop.CancelTimeout(connection.ConnectionRetryBackoffId);
            EventLoop.CancelTimeout(connection.ReadTimeoutId);
            if (!_connections.ContainsKey(connection.DestinationPort))
            {
                return;
            }
            if (!_connections[connection.DestinationPort].Remove(connection.ConnectionId))
            {
                return;
            }
            if (_connections[connection.DestinationPort].Count == 0)
            {
                _connections.Remove(connection.DestinationPort);
            }
            connection.AllocateConnectionCallback?.Invoke(e ?? new Exception("connect error"), null);
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
            if (pdu.PduType == LocalhostUdpDatagram.PduTypeSyn)
            {
                if (!_connections.ContainsKey(port))
                {
                    _connections.Add(port, new Dictionary<string, LocalhostUdpConnection>());
                }
                if (!_connections[port].ContainsKey(pdu.ConnectionId))
                {
                    var newConnection = new LocalhostUdpConnection
                    {
                        ConnectionId = pdu.ConnectionId,
                        DestinationPort = port,
                        Established = true
                    };
                    _connections[port].Add(pdu.ConnectionId, newConnection);
                    ResetReadTimeout(newConnection);
                }
                var connection = _connections[port][pdu.ConnectionId];
                connection.LastReadTime = EventLoop.CurrentTimestamp;
                // always send a syn ack even if connection has already been established.
                var connectionConfirmationPdu = new LocalhostUdpDatagram
                {
                    ConnectionId = pdu.ConnectionId,
                    PduType = LocalhostUdpDatagram.PduTypeSynAck,
                    Version = LocalhostUdpDatagram.Version01
                };
                SendMessage(connection, connectionConfirmationPdu, null);
            }
            else
            {
                if (!_connections.ContainsKey(port) || !_connections[port].ContainsKey(pdu.ConnectionId))
                {
                    // ignore pdus from previous connections which no longer exist.
                    return;
                }
                var connection = _connections[port][pdu.ConnectionId];
                connection.LastReadTime = EventLoop.CurrentTimestamp;
                if (pdu.PduType == LocalhostUdpDatagram.PduTypeSynAck)
                {
                    EstablishConnection(connection);
                }
                else if (pdu.PduType == LocalhostUdpDatagram.PduTypeFin)
                {
                    AbortConnection(connection, new Exception("connection abort"));
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
        }

        private void OnReadTimeout(object obj)
        {
            var connection = (LocalhostUdpConnection)obj;
            var timeSpentSinceLastRead = EventLoop.CurrentTimestamp - connection.LastReadTime;
            if (timeSpentSinceLastRead >= ReadTimeoutMilis)
            {
                AbortConnection(connection, new Exception("read timeout"));
            }
            else
            {
                ResetReadTimeout(connection);
            }
        }

        private void ResetReadTimeout(LocalhostUdpConnection connection)
        {
            EventLoop.CancelTimeout(connection.ReadTimeoutId);
            connection.ReadTimeoutId = EventLoop.ScheduleTimeout(ReadTimeoutMilis,
                OnReadTimeout, connection);
        }

        public void WriteBytesOrSendMessage(object connection, byte[] data, int offset, int length, Action<Exception> cb)
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
                int sent = await _udpClient.SendAsync(pduBytes, pduBytes.Length, "localhost", connection.DestinationPort);
                if (sent != pduBytes.Length)
                {
                    throw new Exception("could not send all bytes in datagram");
                }
                else
                {
                    // NB: Can be sent even if destination port is not bound.
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
