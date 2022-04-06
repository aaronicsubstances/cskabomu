using Kabomu.Common.Abstractions;
using Kabomu.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Common.Components
{
    public class DefaultMessageIdGenerator : IMessageIdGenerator
    {
        private long _lastId;

        public DefaultMessageIdGenerator()
        {
            _lastId = new Random().Next();
        }

        public long NextId()
        {
            Interlocked.Increment(ref _lastId);
            return _lastId;
        }
    }
}
