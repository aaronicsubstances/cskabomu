using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class PredictableMessageIdGenerator : IMessageIdGenerator
    {
        private long _lastId = 0;

        public long NextId()
        {
            return ++_lastId;
        }
    }
}
