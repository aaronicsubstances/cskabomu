using Kabomu.Common.Abstractions;
using Kabomu.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Common.Components
{
    public class SimpleMessageIdGenerator : IMessageIdGenerator
    {
        private long _lastId = 0;

        public long NextId()
        {
            Interlocked.Increment(ref _lastId);
            return _lastId;
        }
    }
}
