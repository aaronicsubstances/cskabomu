using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
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
                new HierarchicalRegistry(null, new RegistryImpl2()));
            Assert.Throws<ArgumentNullException>(() =>
                new HierarchicalRegistry(new RegistryImpl2(), null));
        }

        [Fact]
        public void TestTryGet1()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = -1;
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGet2()
        {
            IRegistry parent = new RegistryImpl2();
            IRegistry child = new RegistryImpl1(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = "t";
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGet1()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = -1;
            Assert.Throws<RegistryException>(() => instance.Get(key));
        }

        [Fact]
        public void TestGet2()
        {
            IRegistry parent = new RegistryImpl2();
            IRegistry child = new RegistryImpl1(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = "t";
            Assert.Throws<RegistryException>(() => instance.Get(key));
        }

        [Fact]
        public void TestGetAndTryGet1()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
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
            IRegistry parent = new RegistryImpl2();
            IRegistry child = new RegistryImpl1(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            ValueTuple<bool, object> expected = (true, "tree");
            var actual = instance.TryGet(key);
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Item2, instance.Get(key));
        }

        [Fact]
        public void TestGetAll1()
        {
            IRegistry parent = new RegistryImpl2();
            IRegistry child = new RegistryImpl1(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = "t";
            var expected = new List<object>();
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll2()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            var expected = new List<object> { 0, "tree" };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll3()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 1;
            var expected = new List<object> { 1, 0, "of" };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll4()
        {
            IRegistry parent = new RegistryImpl2();
            IRegistry child = new RegistryImpl1(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 1;
            var expected = new List<object> { "of", 1, 0 };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetAll5()
        {
            IRegistry parent = new RegistryImpl2();
            IRegistry child = new RegistryImpl1(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 3;
            var expected = new List<object> { 3, 2, 1, 0 };
            var actual = instance.GetAll(key);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst1()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = "key";
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGetFirst(key, _ => (true, null));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst2()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            ValueTuple<bool, object> expected = (false, null);
            var actual = instance.TryGetFirst(key, _ => (false, null));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst3()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 0;
            ValueTuple<bool, object> expected = (true, 0);
            var actual = instance.TryGetFirst(key, x => (true, x));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst4()
        {
            IRegistry parent = new RegistryImpl1(new object[] { "tree", "of", "life" });
            IRegistry child = new RegistryImpl2();
            var instance = new HierarchicalRegistry(parent, child);
            object key = 2;
            ValueTuple<bool, object> expected = (true, 4);
            var actual = instance.TryGetFirst(key, x => (true, (int)x * (int)x));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryGetFirst5()
        {
            IRegistry parent = new RegistryImpl2();
            IRegistry child = new RegistryImpl1(new object[] { "tree", "of", "life" });
            var instance = new HierarchicalRegistry(parent, child);
            object key = 2;
            ValueTuple<bool, object> expected = (true, "LIFE");
            var actual = instance.TryGetFirst(key, x => (true, ((string)x).ToUpper()));
            Assert.Equal(expected, actual);
        }

        class RegistryImpl1 : IRegistry
        {
            private readonly object[] _array;

            public RegistryImpl1(object[] array)
            {
                _array = array;
            }

            public (bool, object) TryGet(object key)
            {
                if (key is int index)
                {
                    if (_array != null && index >= 0 && index < _array.Length)
                    {
                        return (true, _array[index]);
                    }
                }
                return (false, null);
            }

            public IEnumerable<object> GetAll(object key)
            {
                var result = TryGet(key);
                if (result.Item1)
                {
                    return new List<object> { result.Item2 };
                }
                return Enumerable.Empty<object>();
            }

            public object Get(object key)
            {
                return RegistryUtils.Get(this, key);
            }

            public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
            {
                return RegistryUtils.TryGetFirst(this, key, transformFunction);
            }
        }

        class RegistryImpl2 : IRegistry
        {
            public (bool, object) TryGet(object key)
            {
                var result = GetAll(key);
                if (result.Any())
                {
                    return (true, result.First());
                }
                return (false, null);
            }

            public IEnumerable<object> GetAll(object key)
            {
                if (key is int count)
                {
                    if (count >= 0)
                    {
                        var values = new List<object>();
                        for (int i = count; i >= 0; i--)
                        {
                            values.Add(i);
                        }
                        return values;
                    }
                }
                return Enumerable.Empty<object>();
            }

            public object Get(object key)
            {
                return RegistryUtils.Get(this, key);
            }

            public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
            {
                return RegistryUtils.TryGetFirst(this, key, transformFunction);
            }
        }
    }
}
