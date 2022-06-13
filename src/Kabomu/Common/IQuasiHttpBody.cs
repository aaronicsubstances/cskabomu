using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpBody
    {
        long ContentLength { get; }
        string ContentType { get; }
        void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb);
        void OnEndRead(IMutexApi mutex, Exception e);
    }
}
