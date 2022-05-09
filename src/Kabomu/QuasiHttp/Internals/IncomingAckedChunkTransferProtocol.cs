using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    public class IncomingAckedChunkTransferProtocol : IChunkTransferProtocol
    {
        public IncomingAckedChunkTransferProtocol()
        {

        }

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
