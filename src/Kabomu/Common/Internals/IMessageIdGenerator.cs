using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal interface IMessageIdGenerator
    {
        long NextId();
    }
}
