using Kabomu.Mediator.Registry;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class RegistryExtensionsTest
    {
        [Theory]
        [MemberData(nameof(CreateTestJoinData))]
        public void TestJoin(IRegistry parent, IRegistry child, object key, IEnumerable<object> expected)
        {
            var instance = parent.Join(child);
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestJoinData()
        {
            var testData = new List<object[]>();

            IRegistry parent = new IndexedArrayBasedRegistry(null);
            IRegistry child = new IndexedArrayBasedRegistry(null);
            object key = "t";
            IEnumerable<object> expected = new List<object>();
            testData.Add(new object[] { parent, child, key, expected });

            parent = new IndexedArrayBasedRegistry(new object[] { "a", "b", "c" });
            child = new IndexedArrayBasedRegistry(new object[] { "d", "e" });
            key = 0;
            expected = new List<object> { "d", "a" };
            testData.Add(new object[] { parent, child, key, expected });

            parent = new IndexedArrayBasedRegistry(new object[] { "a", "b", "c" });
            child = new IndexedArrayBasedRegistry(new object[] { "d", "e" });
            key = 1;
            expected = new List<object> { "e", "b" };
            testData.Add(new object[] { parent, child, key, expected });
            testData.Add(new object[] { parent, child, key, expected });

            parent = new IndexedArrayBasedRegistry(new object[] { "a", "b", "c" });
            child = new IndexedArrayBasedRegistry(new object[] { "d", "e" });
            key = 2;
            expected = new List<object> { "c" };
            testData.Add(new object[] { parent, child, key, expected });

            return testData;
        }

        [Fact]
        public void TestJoinForEdgeCases()
        {
            IRegistry parent = null;
            IRegistry child = null;
            Assert.Null(parent.Join(child));

            parent = new DecrementingCounterBasedRegistry();
            child = null;
            Assert.Equal(parent, parent.Join(child));

            parent = null;
            child = new DecrementingCounterBasedRegistry();
            Assert.Equal(child, parent.Join(child));
        }

        [Fact]
        public void TestGet()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { true, 8, "just", -8.5 });
            Assert.True(instance.Get<bool>(0));
            Assert.Equal(true, instance.Get<object>(0));
            Assert.Equal(8, instance.Get<int>(1));
            Assert.Equal(8, instance.Get<object>(1));
            Assert.Equal("just", instance.Get<string>(2));
            Assert.Equal("just", instance.Get<object>(2));
            Assert.Equal(-8.5, instance.Get<double>(3));
            Assert.Equal(-8.5, instance.Get<object>(3));

            Assert.ThrowsAny<Exception>(() => instance.Get<int>(0));
            Assert.ThrowsAny<Exception>(() => instance.Get<string>(3));

            Assert.Throws<NotInRegistryException>(() => instance.Get<object>("t"));
        }

        [Fact]
        public void TestTryGet()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { true, 8, "just", -8.5 });
            Assert.Equal(ValueTuple.Create(true, true), instance.TryGet<bool>(0));
            Assert.Equal(ValueTuple.Create(true, true), instance.TryGet<object>(0));
            Assert.Equal(ValueTuple.Create(true, 8), instance.TryGet<int>(1));
            Assert.Equal(ValueTuple.Create(true, 8), instance.TryGet<object>(1));
            Assert.Equal(ValueTuple.Create(true, "just"), instance.TryGet<string>(2));
            Assert.Equal(ValueTuple.Create(true, "just"), instance.TryGet<object>(2));
            Assert.Equal(ValueTuple.Create(true, -8.5), instance.TryGet<double>(3));
            Assert.Equal(ValueTuple.Create(true, -8.5), instance.TryGet<object>(3));

            Assert.ThrowsAny<Exception>(() => instance.TryGet<int>(0));
            Assert.ThrowsAny<Exception>(() => instance.TryGet<string>(3));

            Assert.Equal(ValueTuple.Create(false, false), instance.TryGet<bool>("t"));
            Assert.Equal(ValueTuple.Create(false, 0), instance.TryGet<int>("t"));
            Assert.Equal(ValueTuple.Create(false, 0.0), instance.TryGet<double>("t"));
            Assert.Equal(ValueTuple.Create(false, (string)null), instance.TryGet<string>("t"));
            Assert.Equal(ValueTuple.Create(false, (object)null), instance.TryGet<object>("t"));
        }

        [Fact]
        public void TestGetAll1()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { true, 8, "just", -8.5 });
            Assert.Equal(new List<bool> { true }, instance.GetAll<bool>(0));
            Assert.Equal(new List<object> { true }, instance.GetAll<object>(0));
            Assert.Equal(new List<int> { 8 }, instance.GetAll<int>(1));
            Assert.Equal(new List<object> { 8 }, instance.GetAll<object>(1));
            Assert.Equal(new List<string> { "just" }, instance.GetAll<object>(2));
            Assert.Equal(new List<object> { "just" }, instance.GetAll<object>(2));
            Assert.Equal(new List<double> { -8.5 }, instance.GetAll<double>(3));
            Assert.Equal(new List<object> { -8.5 }, instance.GetAll<object>(3));

            // force evaluation with ToList() to trigger cast exceptions.
            Assert.ThrowsAny<Exception>(() => instance.GetAll<int>(0).ToList());
            Assert.ThrowsAny<Exception>(() => instance.GetAll<string>(3).ToList());

            Assert.Equal(new List<bool>(), instance.GetAll<bool>("t"));
            Assert.Equal(new List<int>(), instance.GetAll<int>("t"));
            Assert.Equal(new List<double>(), instance.GetAll<double>("t"));
            Assert.Equal(new List<string>(), instance.GetAll<string>("t"));
            Assert.Equal(new List<object>(), instance.GetAll<object>("t"));
        }

        [Fact]
        public void TestGetAll2()
        {
            var instance = new DecrementingCounterBasedRegistry();
            Assert.Equal(new List<int> { 3, 2, 1, 0 }, instance.GetAll<int>(3));
            Assert.Equal(new List<object> { 3, 2, 1, 0 }, instance.GetAll<object>(3));
        }

        [Fact]
        public void TestGetAll3()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { "just", "good", "enough", 3, 4, 5 });
            Assert.Equal(new List<object> { "just", "good", "enough", 3, 4, 5 }, instance.GetAll<object>(null));
        }

        [Fact]
        public void TestGetAll4()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { 3, 4, 5 });
            Assert.Equal(new List<int> { 3, 4, 5 }, instance.GetAll<int>(null));
            Assert.Equal(new List<object> { 3, 4, 5 }, instance.GetAll<object>(null));
        }

        [Fact]
        public void TestTryGetFirst()
        {
            var instance = new IndexedArrayBasedRegistry(new object[] { true, 8, "just", -8.5, "good",
                false, 9, 10, 11 });
            Assert.Equal((true, true), instance.TryGetFirst<bool>(null,
                x => (x is bool, x is bool ? (bool)x : false)));
            Assert.Equal((true, false), instance.TryGetFirst<object>(null,
                x => (x is bool && !((bool)x), x is bool ? (bool)x : false)));
            Assert.Equal((false, null), instance.TryGetFirst<object>(null,
                x => (false, "tree")));
            Assert.Equal((false, false), instance.TryGetFirst<bool>(null,
                x => (false, true)));
            Assert.Equal((true, 24), instance.TryGetFirst<int>(1,
                x => (true, ((int)x) * 3)));
            Assert.Equal((true, 2.75), instance.TryGetFirst<double>(null,
                x => (x is int && ((int)x) > 10, x is int ? ((int)x) / 4.0 : 0.0)));
            Assert.Equal((false, 0.0), instance.TryGetFirst<double>(null,
                x => (x is int && ((int)x) < 2, x is int ? ((int)x) * 4.0 : -10.0)));
        }

        [Fact]
        public void TestAddLazy()
        {
            var expectedKey = "animal";
            var expectedValue = "goat";
            var instance = new ErrorBasedMutableRegistry();
            var cbCalled = false;
            Func<object> valueGenerator = () =>
            {
                Assert.False(cbCalled);
                cbCalled = true;
                return expectedValue;
            };
            Assert.Equal(instance, instance.AddLazy(expectedKey, valueGenerator));


            Assert.Equal(expectedKey, instance.ActualKeyAdded);
            Assert.NotNull(instance.ActualValueGeneratorAdded);
            Assert.Equal(expectedValue, instance.ActualValueGeneratorAdded.Invoke());
            // test again, that another invocation still gives the same value.
            Assert.Equal(expectedValue, instance.ActualValueGeneratorAdded.Invoke());
        }
    }
}
