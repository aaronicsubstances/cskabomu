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
        public void Test1()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            CommonRegistryTestRunner.TestOps(instance, "t", new List<object>());
        }

        [Fact]
        public void Test2()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            CommonRegistryTestRunner.TestOps(instance, 0, new List<object> { 0, "tree" });
        }

        [Fact]
        public void Test3()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            CommonRegistryTestRunner.TestOps(instance, 1, new List<object> { 1, 0, "of" });
        }

        [Fact]
        public void Test4()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            CommonRegistryTestRunner.TestOps(instance, 1, new List<object> { "of", 1, 0 });
        }

        [Fact]
        public void Test5()
        {
            IRegistry parent = new DecrementingCounterBasedRegistry();
            IRegistry child = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            CommonRegistryTestRunner.TestOps(instance, 3, new List<object> { 3, 2, 1, 0 });
        }

        [Fact]
        public void Test6()
        {
            IRegistry descendant1 = new DecrementingCounterBasedRegistry();
            IRegistry descendant2 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2),
                descendant3);
            CommonRegistryTestRunner.TestOps(instance, 2,
                new List<object> { 2, 1, 0, "life", 2, 1, 0 });
        }

        [Fact]
        public void Test7()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(new object[] { "zero", "one", "two" });
            IRegistry descendant2 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(descendant1, new HierarchicalRegistry(descendant2,
                descendant3));
            CommonRegistryTestRunner.TestOps(instance, 2,
                new List<object> { 2, 1, 0, "life", "two" });
        }

        [Fact]
        public void Test8()
        {
            IRegistry descendant1 = new IndexedArrayBasedRegistry(new object[] { "zero", "one", "two" });
            IRegistry descendant2 = new DecrementingCounterBasedRegistry();
            IRegistry descendant3 = new DecrementingCounterBasedRegistry();
            IRegistry descendant4 = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(new HierarchicalRegistry(descendant1, descendant2), 
                new HierarchicalRegistry(descendant3, descendant4));
            CommonRegistryTestRunner.TestOps(instance, 1,
                new List<object> { "of", 1, 0, 1, 0, "one" });
        }

        [Fact]
        public void Test9()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            CommonRegistryTestRunner.TestOps(instance, "non-existent key", new List<object>());
        }

        [Fact]
        public void Test10()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            CommonRegistryTestRunner.TestOps(instance, 0, new List<object> { 0, "tree" });
        }

        [Fact]
        public void TestTryGetFirst1()
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
        public void TestTryGetFirst2()
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
        public void TestTryGetFirst3()
        {
            IRegistry parent = new IndexedArrayBasedRegistry(new object[] { "tree", "of", "life" });
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 2;
            ValueTuple<bool, object> expected = (true, 3);
            var actual = instance.TryGetFirst(key, x => ((int)x < 2, ((int)x) * 3));
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// test that once child contains a value, the parent is not even contacted.
        /// </summary>
        [Fact]
        public void TestTryGetFirst4()
        {
            IRegistry parent = new TempMutableRegistry();
            IRegistry child = new DecrementingCounterBasedRegistry();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 2;
            ValueTuple<bool, object> expected = (true, 2);
            var actual = instance.TryGetFirst(key, x => (true, x));
            Assert.Equal(expected, actual);
        }
    }
}
