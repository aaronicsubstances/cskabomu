using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class CompositeByteQueue : IIntQueue
    {
        public CompositeByteQueue(IByteQueue startChild, IByteQueue endChild)
        {
            StartChild = startChild;
            EndChild = endChild;
        }

        public int Capacity => throw new NotImplementedException();

        public int Position => throw new NotImplementedException();

        public IByteQueue StartChild { get; }
        public IByteQueue EndChild { get; }

        public void DoneReading()
        {
            throw new NotImplementedException();
        }

        public int MultiRead(byte[] dest, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public int Read()
        {
            throw new NotImplementedException();
        }
        public byte ReadUint8()
        {
            throw new NotImplementedException();
        }

        public sbyte ReadSint8()
        {
            throw new NotImplementedException();
        }

        public short ReadSint16be()
        {
            throw new NotImplementedException();
        }

        public int ReadSint32be()
        {
            throw new NotImplementedException();
        }

        public long ReadSint64be()
        {
            throw new NotImplementedException();
        }

        public short ReadUint16be()
        {
            throw new NotImplementedException();
        }

        public int ReadUint32be()
        {
            throw new NotImplementedException();
        }

        public long ReadUint64be()
        {
            throw new NotImplementedException();
        }
    }
}
