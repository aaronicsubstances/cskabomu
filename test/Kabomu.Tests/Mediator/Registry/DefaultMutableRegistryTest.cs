using Kabomu.Mediator.Registry;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class DefaultMutableRegistryTest
    {
        [Fact]
        public void Test1()
        {
            var instance = new DefaultMutableRegistry();
            CommonRegistryTestRunner.TestOps(instance, "one", new List<object>());

            instance.Add("one", 1);
            instance.AddGenerator("one", () => "baako");
            CommonRegistryTestRunner.TestOps(instance, "one", new List<object> { "baako", 1 });

            instance.Remove("non-existent");
            CommonRegistryTestRunner.TestOps(instance, "one", new List<object> { "baako", 1 });

            instance.Remove("one");
            CommonRegistryTestRunner.TestOps(instance, "one", new List<object>());

            instance.Add("two", 2);
            CommonRegistryTestRunner.TestOps(instance, "two", new List<object> { 2 });

            instance.Remove("two");
            CommonRegistryTestRunner.TestOps(instance, "two", new List<object>());

            instance.AddGenerator("two", () => "2");
            instance.AddGenerator("two", () => "mmienu");
            instance.Add("two", 2);
            CommonRegistryTestRunner.TestOps(instance, "two", new List<object> { 2, "mmienu", "2" });
        }

        [Fact]
        public void Test2()
        {
            var instance = new DefaultMutableRegistry();
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(3),
                new List<object>());

            instance.Add("one", 1);
            instance.AddGenerator("one", () => "baako");
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(3),
                new List<object> { "baako", 1 });

            instance.Remove("non-existent");
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(3),
                new List<object> { "baako", 1 });

            instance.Add(10, 100);
            CommonRegistryTestRunner.TestOps(instance, 10,
                new List<object> { 100 });
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(4),
                new List<object>());

            instance.AddGenerator("four", () => "4");
            instance.AddGenerator("four", () => "nnan");
            instance.Add("four", 4);
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(4),
                new List<object> { 4, "nnan", "4" });

            instance.Remove("one");
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(3),
                new List<object>());
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(4),
                new List<object> { 4, "nnan", "4" });
            CommonRegistryTestRunner.TestOps(instance, 10,
                new List<object> { 100 });

            instance.Remove("four");
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(3),
                new List<object>());
            CommonRegistryTestRunner.TestOps(instance, new TestRegistryKeyPattern(4),
                new List<object>());
            CommonRegistryTestRunner.TestOps(instance, 10,
                new List<object> { 100 });
        }

        class TestRegistryKeyPattern : IRegistryKeyPattern
        {
            private readonly int _strKeyLen;

            public TestRegistryKeyPattern(int strKeyLen)
            {
                _strKeyLen = strKeyLen;
            }

            public bool IsMatch(object input)
            {
                return input is string && ((string)input).Length == _strKeyLen;
            }
        }
    }
}
