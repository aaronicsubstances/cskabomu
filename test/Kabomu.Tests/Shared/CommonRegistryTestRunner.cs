using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public static class CommonRegistryTestRunner
    {
        public static void TestOps(IRegistry instance, object key, IEnumerable<object> expected)
        {
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
            if (expected.Any())
            {
                var first = expected.First();
                Assert.Equal((true, first), instance.TryGet(key));
                Assert.Equal(first, instance.Get(key));
                Assert.Equal((true, first), instance.TryGetFirst(key, x => (true, x)));

                var doubleArrayExpectation = new object[] { first, first };
                var doubleArrayResult = instance.TryGetFirst(key, x => (true, new object[] { x, x }));
                Assert.True(doubleArrayResult.Item1);
                Assert.Equal(doubleArrayExpectation, doubleArrayResult.Item2);
            }
            else
            {
                Assert.Equal((false, null), instance.TryGet(key));
                Assert.Throws<NotInRegistryException>(() => instance.Get(key));
            }
            Assert.Equal((false, null), instance.TryGetFirst(key, _ => (false, null)));
        }
    }
}
