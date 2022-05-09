using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class AckedTransferProtocol
    {
        private byte[] _buffer = new byte[8192];

        public AckedTransferProtocol(bool isResponseTransfer, IQuasiHttpBody body,
            IQuasiHttpTransport transport, object connection)
        {
            IsResponseTransfer = isResponseTransfer;
            Body = body;
            Transport = transport;
            Connection = connection;
        }

        public bool IsResponseTransfer { get; }
        public IQuasiHttpBody Body { get; }
        public IQuasiHttpTransport Transport { get; }
        public object Connection { get; }

        public void Start()
        {
            HandleWriteOutcome(null);
        }

        private void HandleWriteOutcome(Exception e)
        {
            if (e != null)
            {
                Transport.ReleaseConnection(Connection);
                Body.OnEndRead(e);
                return;
            }
            try
            {
                Body.OnDataRead(_buffer, 0, _buffer.Length, HandleReadOutcome);
            }
            catch (Exception e2)
            {
                Transport.ReleaseConnection(Connection);
                Body.OnEndRead(e2);
            }
        }

        private void HandleReadOutcome(Exception e, int bytesRead)
        {
            if (e != null)
            {
                Transport.ReleaseConnection(Connection);
                Body.OnEndRead(e);
                return;
            }
            if (bytesRead == 0)
            {
                if (IsResponseTransfer)
                {
                    Transport.ReleaseConnection(Connection);
                }
                Body.OnEndRead(null);
                return;
            }
            try
            {
                Transport.Write(Connection, _buffer, 0, bytesRead, HandleWriteOutcome);
            }
            catch (Exception e2)
            {
                Transport.ReleaseConnection(Connection);
                Body.OnEndRead(e2);
            }
        }
    }
}
