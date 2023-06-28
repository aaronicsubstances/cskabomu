using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class MutableInt
    {
        public int Value;

        public MutableInt()
        {

        }

        public MutableInt(int value)
        {
            Value = value;
        }

        public void Increment()
        {
            Value++;
        }

        public override bool Equals(object obj)
        {
            return obj is MutableInt @int &&
                   Value == @int.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

    }
}
