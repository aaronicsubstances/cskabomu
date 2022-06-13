using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpBody : IQuasiHttpBody
    {
        public Action<IMutexApi, byte[], int, int, Action<Exception, int>> ReadBytesCallback { get; set; }

        public long ContentLength { get; set; }
        public string ContentType { get; set; }

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            ReadBytesCallback?.Invoke(mutex, data, offset, bytesToRead, cb);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
        }
    }
}
