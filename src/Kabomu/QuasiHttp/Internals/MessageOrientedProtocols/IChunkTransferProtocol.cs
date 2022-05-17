using System;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
{
    internal interface IChunkTransferProtocol
    {
        IQuasiHttpBody Body { get; }
        void Cancel(Exception e);
        void ProcessChunkGetPdu(int bytesToRead);
        void ProcessChunkRetPdu(byte[] data, int offset, int length);
    }
}