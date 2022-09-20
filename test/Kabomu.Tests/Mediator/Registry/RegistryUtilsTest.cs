using Kabomu.Mediator.Registry;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class RegistryUtilsTest
    {
        [Fact]
        public void TestGet1()
        {
            var instance = new IndexedArrayBasedRegistry(null);
            object key = "t";
            Assert.Throws<NotInRegistryException>(() => RegistryUtils.Get(instance, key));
        }

        [Fact]
        public void TestGet2()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { "t" });
            object key = 0;
            object expected = "t";
            var actual = RegistryUtils.Get(instance, key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst1()
        {
            var instance = new IndexedArrayBasedRegistry(null);
            object key = "t";
            Func<object, (bool, object)> transformFunction = _ => throw new NotImplementedException();
            ValueTuple<bool, object> expected = (false, null);
            var actual = RegistryUtils.TryGetFirst(instance, key, transformFunction);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst2()
        {
            var instance = new DecrementingCounterBasedRegistry();
            object key = 20;
            Func<object, (bool, object)> transformFunction = x => ValueTuple.Create(((int)x) % 8 == 0, x);
            ValueTuple<bool, object> expected = (true, 16);
            var actual = RegistryUtils.TryGetFirst(instance, key, transformFunction);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst3()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { "r", "s" });
            object key = null;
            Func<object, (bool, object)> transformFunction = x => ValueTuple.Create(x == null, x);
            ValueTuple<bool, object> expected = (false, null);
            var actual = RegistryUtils.TryGetFirst(instance, key, transformFunction);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst4()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { null,  "s" });
            object key = null;
            Func<object, (bool, object)> transformFunction = x => ValueTuple.Create(x != null, x);
            ValueTuple<bool, object> expected = (true, "s");
            var actual = RegistryUtils.TryGetFirst(instance, key, transformFunction);
            Assert.Equal(expected, actual);
        }
    }
}
