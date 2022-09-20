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
        public static void TestReadonlyOps(IRegistry instance, object key, IEnumerable<object> expected)
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

            var firstNonNullItem = expected.FirstOrDefault(x => x != null);
            var nonNullSearchResult = instance.TryGetFirst(key, x => (x != null, x));
            Assert.Equal(nonNullSearchResult.Item1, expected.Any(x => x != null));
            Assert.Equal(firstNonNullItem, nonNullSearchResult.Item2);
        }

        public static void TestMutableOps(IMutableRegistry instance)
        {
            TestReadonlyOps(instance, "one", new List<object>());

            instance.Add("one", 1);
            instance.AddGenerator("one", () => "baako");
            TestReadonlyOps(instance, "one", new List<object> { "baako", 1 });

            instance.Remove("non-existent");
            TestReadonlyOps(instance, "one", new List<object> { "baako", 1 });

            instance.Remove("one");
            TestReadonlyOps(instance, "one", new List<object>());

            instance.Add("two", 2);
            TestReadonlyOps(instance, "two", new List<object> { 2 });

            instance.Remove("two");
            TestReadonlyOps(instance, "two", new List<object>());

            instance.AddGenerator("two", () => "2");
            instance.Add("two", null);
            instance.AddLazy("two", () => "mmienu");
            instance.Add("two", 2);
            instance.Add("two", null);
            TestReadonlyOps(instance, "two", new List<object> { null, 2, "mmienu", null, "2" });

            instance.Add("two", null);
            TestReadonlyOps(instance, "two", new List<object> { null, null, 2, "mmienu", null, "2" });
        }
    }
}
