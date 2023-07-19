﻿using Kabomu.Mediator.Registry;
using Kabomu.Tests.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class DefaultMutableRegistryTest
    {
        [Fact]
        public void Test1()
        {
            var instance = new DefaultMutableRegistry();
            CommonRegistryTestRunner.TestMutableOps(instance);
        }
    }
}
