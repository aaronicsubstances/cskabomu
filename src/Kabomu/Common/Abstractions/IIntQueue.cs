using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IIntQueue : IByteQueue
    {
        byte ReadUint8();
        sbyte ReadSint8();
        ushort ReadUint16be();
        short ReadSint16be();
        uint ReadUint32be();
        int ReadSint32be();
        ulong ReadUint64be();
        long ReadSint64be();
    }
}
