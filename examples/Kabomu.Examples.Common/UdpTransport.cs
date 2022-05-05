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

        public bool SerializingEnabled => true;

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
            var datagram = SerializePdu(pdu);
            try
            {
                int sent = await _udpClient.SendAsync(datagram, datagram.Length, remoteEndpoint);
                if (sent != datagram.Length)
                {
                    throw new Exception("sent less bytes");
                }
                cb.Invoke(null);
            }
            catch (Exception e)
            {
                cb.Invoke(e);
                ErrorHandler?.Invoke(e, "error encountered during sending");
            }
        }

        public void SendSerializedPdu(byte[] data, int offset, int length, 
            object connectionHandleOrRemoteEndpoint, Action<Exception> cb)
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
                    var pdu = DeserializePdu(datagram.Buffer, datagram.Buffer.Length);
                    Upstream.ReceiveDeserializedPdu(pdu, datagram.RemoteEndPoint);
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

        private byte[] SerializePdu(QuasiHttpPdu pdu)
        {
            var csvData = new List<List<string>>();
            csvData.Add(new List<string> { pdu.Version.ToString() });
            csvData.Add(new List<string> { pdu.PduType.ToString() });
            csvData.Add(new List<string> { pdu.Flags.ToString() });
            csvData.Add(new List<string> { pdu.RequestId.ToString() });
            csvData.Add(new List<string> { "", (pdu.Path ?? "").ToString() });
            csvData.Add(new List<string> { pdu.StatusIndicatesSuccess.ToString() });
            csvData.Add(new List<string> { pdu.StatusIndicatesClientError.ToString() });
            csvData.Add(new List<string> { "", (pdu.StatusMessage ?? "").ToString() });
            csvData.Add(new List<string> { pdu.ContentLength.ToString() });
            csvData.Add(new List<string> { "", (pdu.ContentType ?? "").ToString() });
            csvData.Add(new List<string> { "", Convert.ToBase64String(pdu.Data ?? new byte[0], pdu.DataOffset, pdu.DataLength) });
            foreach (var header in (pdu.Headers ?? new QuasiHttpKeyValueCollection()).Content)
            {
                var headerRow = new List<string> { header.Key };
                headerRow.AddRange(header.Value);
                csvData.Add(headerRow);
            }
            var csv = CsvUtils.Serialize(csvData);
            return ByteUtils.StringToBytes(csv);
        }

        private QuasiHttpPdu DeserializePdu(byte[] datagram, int length)
        {
            var pdu = new QuasiHttpPdu();

            // NB: after serialization csv doesn't distinguish between an empty list and
            // a singleton list containing an empty string.
            // So add enough columns as necessary to prevent errors.
            var csv = ByteUtils.BytesToString(datagram, 0, length);
            var csvData = CsvUtils.Deserialize(csv);
            pdu.Version = byte.Parse(csvData[0][0]);
            pdu.PduType = byte.Parse(csvData[1][0]);
            pdu.Flags = byte.Parse(csvData[2][0]);
            pdu.RequestId = int.Parse(csvData[3][0]);
            pdu.Path = csvData[4][1];
            pdu.StatusIndicatesSuccess = bool.Parse(csvData[5][0]);
            pdu.StatusIndicatesClientError = bool.Parse(csvData[6][0]);
            pdu.StatusMessage = csvData[7][1];
            pdu.ContentLength = int.Parse(csvData[8][0]);
            pdu.ContentType = csvData[9][1];
            pdu.Data = Convert.FromBase64String(csvData[10][1]);
            pdu.DataLength = pdu.Data.Length;
            for (int i = 11; i < csvData.Count; i++)
            {
                var headerRow = csvData[i];
                var headerValue = new List<string>(headerRow.GetRange(1, headerRow.Count - 1));
                if (pdu.Headers == null)
                {
                    pdu.Headers = new QuasiHttpKeyValueCollection();
                }
                pdu.Headers.Content.Add(headerRow[0], headerValue);
            }
            return pdu;
        }
    }
}
