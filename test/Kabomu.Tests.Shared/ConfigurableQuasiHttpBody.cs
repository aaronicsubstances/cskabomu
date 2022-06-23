﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpBody : IQuasiHttpBody
    {
        public Func<byte[], int, int, Task<int>> ReadBytesCallback { get; set; }

        public long ContentLength { get; set; }
        public string ContentType { get; set; }

        public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            return ReadBytesCallback.Invoke(data, offset, bytesToRead);
        }

        public Task EndRead(Exception e)
        {
            return Task.CompletedTask;
        }
    }
}
