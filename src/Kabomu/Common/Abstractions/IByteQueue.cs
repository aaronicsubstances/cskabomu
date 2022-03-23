using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IByteQueue
    {
        int Capacity { get; }
        int Position { get; }
        int Read();
        int MultiRead(byte[] dest, int offset, int length);
        void DoneReading();
    }
}
