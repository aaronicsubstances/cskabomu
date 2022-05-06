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

        public int MaxPduPayloadSize => 30_000;

        public bool DirectSendRequestProcessingEnabled => false;

        public void ProcessSendRequest(QuasiHttpRequestMessage request, object connectionHandleOrRemoteEndpoint, 
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            throw new NotImplementedException();
        }

        public async void SendPdu(QuasiHttpPdu pdu, object connectionHandleOrRemoteEndpoint, 
            Action<Exception> cb)
        {
            IPEndPoint remoteEndpoint;
            if (connectionHandleOrRemoteEndpoint is int)
            {
                remoteEndpoint = new IPEndPoint(IPAddress.Loopback, (int)connectionHandleOrRemoteEndpoint);
            }
            else
            {
                remoteEndpoint = (IPEndPoint)connectionHandleOrRemoteEndpoint;
            }
            var datagram = pdu.Serialize();
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
                    var pdu = QuasiHttpPdu.Deserialize(datagram.Buffer, 0, datagram.Buffer.Length);
                    Upstream.ReceivePdu(pdu, datagram.RemoteEndPoint);
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
    }
}
