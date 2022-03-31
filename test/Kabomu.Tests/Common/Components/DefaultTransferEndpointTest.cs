using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Components
{
    public class DefaultTransferEndpointTest
    {
        [Fact]
        public void TestToString()
        {
            var instance = new DefaultTransferEndpoint();
            Assert.Equal(0, instance.Id);
            Assert.Null(instance.Name);
            Assert.Equal(":0", instance.ToString());

            instance.Name = "Accra";
            instance.Id = 8;
            Assert.Equal("Accra", instance.Name);
            Assert.Equal(8, instance.Id);
            Assert.Equal("Accra:8", instance.ToString());
        }
    }
}
