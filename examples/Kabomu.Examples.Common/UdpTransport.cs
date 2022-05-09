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

        public UdpTransport(int port, CancellationToken cancellationToken)
        {
            _udpClient = new UdpClient(port);
            _cancellationToken = cancellationToken;
        }

        public IQuasiHttpClient Upstream { get; set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public int MaximumChunkSize => 30_000;

        public bool DirectSendRequestProcessingEnabled => false;

        public bool IsChunkDeliveryAcknowledged => false;

        public async void Write(object connection, byte[] data, int offset, int length, 
            Action<Exception> cb)
        {
            IPEndPoint remoteEndpoint;
            if (connection is int)
            {
                remoteEndpoint = new IPEndPoint(IPAddress.Loopback, (int)connection);
            }
            else
            {
                remoteEndpoint = (IPEndPoint)connection;
            }
            var datagram = Serialize(connection, data, offset, length);
            try
            {
                int sent = await _udpClient.SendAsync(datagram, datagram.Length, remoteEndpoint);
                if (sent != datagram.Length)
                {
                    throw new Exception("sent less bytes");
                }
                else
                {
                    // NB: Can be sent even if target port is not bound.
                }
                cb.Invoke(null);
            }
            catch (Exception e)
            {
                cb.Invoke(e);
                ErrorHandler?.Invoke(e, "error encountered during sending");
            }
        }

        public async void Start()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var datagram = await _udpClient.ReceiveAsync();
                    object[] connectionAndHeaderLen = new object[2];
                    Deserialize(datagram.Buffer, 0, connectionAndHeaderLen);
                    var connection = connectionAndHeaderLen[0];
                    int headerLen = (int)connectionAndHeaderLen[1];
                    Upstream.OnReceive(connection, datagram.Buffer, headerLen, datagram.Buffer.Length - headerLen);
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

        private byte[] Serialize(object connection, byte[] data, int offset, int length)
        {
            var guid = (string)connection;
            var datagram = new byte[33 + length];
            datagram[0] = 0;
            Array.Copy(ByteUtils.StringToBytes(guid), 0, datagram, 1, 32);
            Array.Copy(data, offset, datagram, 0, length);
            return datagram;
        }

        private void Deserialize(byte[] data, int offset, object[] connectionAndHeaderLen)
        {
            connectionAndHeaderLen[0] = ByteUtils.BytesToString(data, offset + 1, 32);
            connectionAndHeaderLen[1] = 33;
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var guid = Guid.NewGuid().ToString("n");
            cb.Invoke(null, guid);
        }

        public void ReleaseConnection(object connection)
        {
            // nothing to do.
        }

        public void Read(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            throw new NotImplementedException();
        }

        public void ProcessSendRequest(object remoteEndpoint, QuasiHttpRequestMessage request,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            throw new NotImplementedException();
        }
    }
}
