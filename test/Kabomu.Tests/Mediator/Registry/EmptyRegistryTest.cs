using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class EmptyRegistryTest
    {
        [Fact]
        public void TestTryGet()
        {
            ValueTuple<bool, object> expected = (false, null);
            var actual = EmptyRegistry.Instance.TryGet("key");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll()
        {
            var expected = new List<object>();
            var actual = EmptyRegistry.Instance.GetAll("key");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst()
        {
            ValueTuple<bool, object> expected = (false, null);
            var actual = EmptyRegistry.Instance.TryGetFirst("key", _ => (true, null));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGet()
        {
            Assert.Throws<RegistryException>(() => EmptyRegistry.Instance.Get("key"));
        }
    }
}
