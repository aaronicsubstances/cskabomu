using Kabomu.Mediator.Registry;
using Kabomu.Tests.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class EmptyRegistryTest
    {
        [Fact]
        public void Test1()
        {
            CommonRegistryTestRunner.TestReadonlyOps(EmptyRegistry.Instance, "any", new List<object>());
        }
    }
}
