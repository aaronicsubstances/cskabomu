﻿using System.Collections.Generic;

namespace Kabomu.Tests.Common
{
    public class FakeUdpTransportHub
    {
        public Dictionary<object, FakeUdpTransport> Connections { get; } = new Dictionary<object, FakeUdpTransport>();
    }
}