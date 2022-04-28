using Kabomu.QuasiHttp.Internals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.TestHelpers
{
    public class PredictableMessageIdGenerator : IRequestIdGenerator
    {
        private int _lastId = 0;

        public int NextId()
        {
            return ++_lastId;
        }
    }
}
