using Kabomu.Mediator.Registry;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class HierarchicalRegistryTest
    {
        [Fact]
        public void TestErrorsInConstruction()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HierarchicalRegistry(null, new DecrementingCounterBasedRegistry()));
            Assert.Throws<ArgumentNullException>(() =>
                new HierarchicalRegistry(new DecrementingCounterBasedRegistry(), null));
        }

        [Fact]
        public void TestTryGet1()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = -1;
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGet2()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = "t";
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGet1()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = -1;
            Assert.Throws<NotInRegistryException>(() => instance.Get(key));
        }

        [Fact]
        public void TestGet2()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = "t";
            Assert.Throws<NotInRegistryException>(() => instance.Get(key));
        }

        [Fact]
        public void TestGet3()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(null);
            IRegistry descendant2 = new IndexedArrayBasedRegistry(null);
            IRegistry descendant3 = new IndexedArrayBasedRegistry(null);
            IRegistry descendant4 = new IndexedArrayBasedRegistry(null);
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2),
                new HierarchicalRegistry(descendant3, descendant4));
            object key = "t";
            Assert.Throws<NotInRegistryException>(() => instance.Get(key));
        }

        [Fact]
        public void TestGet4()
        {
            IRegistry descendant1 = new DecrementingCounterBasedRegistry();
            IRegistry descendant2 = new IndexedArrayBasedRegistry(null);
            IRegistry descendant3 = new IndexedArrayBasedRegistry(null);
            IRegistry descendant4 = new IndexedArrayBasedRegistry(new object[] { "t" });
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2),
                new HierarchicalRegistry(descendant3, descendant4));
            object key = 0;
            object expected = "t";
            var actual = instance.Get(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGet5()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(new object[] { "t" });
            IRegistry descendant2 = new IndexedArrayBasedRegistry(null);
            IRegistry descendant3 = new IndexedArrayBasedRegistry(null);
            IRegistry descendant4 = new IndexedArrayBasedRegistry(null);
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2),
                new HierarchicalRegistry(descendant3, descendant4));
            object key = 0;
            object expected = "t";
            var actual = instance.Get(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAndTryGet1()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            ValueTuple<bool, object> expected = (true, 0);
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Item2, instance.Get(key));
        }

        [Fact]
        public void TestGetAndTryGet2()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            ValueTuple<bool, object> expected = (true, "tree");
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Item2, instance.Get(key));
        }

        [Fact]
        public void TestGetAndTryGet3()
        {
            IRegistry descendant1 = new DecrementingCounterBasedRegistry();
            IRegistry descendant2 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2),
                descendant3);
            object key = 2;
            ValueTuple<bool, object> expected = (true, 2);
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Item2, instance.Get(key));
        }

        [Fact]
        public void TestGetAndTryGet4()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(new object[] { "zero", "one", "two" });
            IRegistry descendant2 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(descendant1, new HierarchicalRegistry(descendant2,
                descendant3));
            object key = 2;
            ValueTuple<bool, object> expected = (true, 2);
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Item2, instance.Get(key));
        }

        [Fact]
        public void TestGetAndTryGet5()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(new object[] { "zero", "one", "two" });
            IRegistry descendant2 = new DecrementingCounterBasedRegistry();
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            IRegistry descendant4 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2),
                new HierarchicalRegistry(descendant3, descendant4));
            object key = 1;
            ValueTuple<bool, object> expected = (true, "of");
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Item2, instance.Get(key));
        }

        [Fact]
        public void TestGetAll1()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = "t";
            var expected = new List<object>();
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll2()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            var expected = new List<object> { 0, "tree" };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll3()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 1;
            var expected = new List<object> { 1, 0, "of" };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll4()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 1;
            var expected = new List<object> { "of", 1, 0 };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll5()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 3;
            var expected = new List<object> { 3, 2, 1, 0 };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll6()
        {
            IRegistry descendant1 = new DecrementingCounterBasedRegistry();
            IRegistry descendant2 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2),
                descendant3);
            object key = 2;
            var expected = new List<object> { 2, 1, 0, "life", 2, 1, 0 };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll7()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(new object[] { "zero", "one", "two" });
            IRegistry descendant2 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(descendant1, new HierarchicalRegistry(descendant2,
                descendant3));
            object key = 2;
            var expected = new List<object> { 2, 1, 0, "life", "two" };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll8()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(new object[] { "zero", "one", "two" });
            IRegistry descendant2 = new DecrementingCounterBasedRegistry();
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            IRegistry descendant4 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2), 
                new HierarchicalRegistry(descendant3, descendant4));
            object key = 1;
            var expected = new List<object> { "of", 1, 0, 1, 0, "one" };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst1()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = "key";
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGetFirst(key, _ => (true, null));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst2()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGetFirst(key, _ => (false, null));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst3()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            ValueTuple<bool, object> expected = (true, 0);
            var actual = instance.TryGetFirst(key, x => (true, x));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst4()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 2;
            ValueTuple<bool, object> expected = (true, 4);
            var actual = instance.TryGetFirst(key, x => (true, (int)x * (int)x));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst5()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 2;
            ValueTuple<bool, object> expected = (true, "LIFE");
            var actual = instance.TryGetFirst(key, x => (true, ((string)x).ToUpper()));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst6()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 2;
            ValueTuple<bool, object> expected = (true, 3);
            var actual = instance.TryGetFirst(key, x => ((int)x < 2, ((int)x) * 3));
            Assert.Equal(expected, actual);
        }
    }
}
