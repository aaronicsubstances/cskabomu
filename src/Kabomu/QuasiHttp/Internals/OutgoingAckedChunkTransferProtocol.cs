using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class OutgoingAckedChunkTransferProtocol : IChunkTransferProtocol
    {
        public OutgoingAckedChunkTransferProtocol(ITransferProtocol transferProtocol, Transfer transfer, IQuasiHttpBody body)
        {
            TransferProtocol = transferProtocol;
            Transfer = transfer;
            Body = body;
        }

        public ITransferProtocol TransferProtocol { get; }
        public Transfer Transfer { get; }
        public IQuasiHttpBody Body { get; }

        public void Cancel(Exception e)
        {
            throw new NotImplementedException();
        }

        public void ProcessChunkGetPdu(int bytesToRead)
        {
            throw new NotImplementedException();
        }

        public void ProcessChunkRetPdu(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }
    }
}
