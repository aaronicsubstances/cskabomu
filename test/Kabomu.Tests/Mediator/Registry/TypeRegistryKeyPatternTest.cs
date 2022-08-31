using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class TypeRegistryKeyPatternTest
    {
        [Fact]
        public void TestErrorsInConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new TypeRegistryKeyPattern(null));
        }

        [Theory]
        [MemberData(nameof(CreateTestIsMatchData))]
        public void TestIsMatch(Type t, object input, bool expected)
        {
            var instance = new TypeRegistryKeyPattern(t);
            var actual = instance.IsMatch(input);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestIsMatchData()
        {
            return new List<object[]>
            {
                new object[]{ typeof(IRegistry), null, false },
                new object[]{ typeof(IRegistry), 78, false },
                new object[]{ typeof(IRegistry), typeof(string), false },
                new object[]{ typeof(IRegistry), typeof(EmptyRegistry), true },
                new object[]{ typeof(IRegistry), new TypeBasedRegistryKey(typeof(EmptyRegistry)), true },
                new object[]{ typeof(EmptyRegistry), typeof(DefaultMutableRegistry), false },
                new object[]{ typeof(EmptyRegistry), typeof(EmptyRegistry), true },
                new object[]{ typeof(EmptyRegistry), new TypeBasedRegistryKey(typeof(EmptyRegistry)), true },
                new object[]{ typeof(EmptyRegistry), new TypeBasedRegistryKey(typeof(IRegistry)), false },
                new object[]{ typeof(string), typeof(string), true },
                new object[]{ typeof(string), 4, false },
                new object[]{ typeof(string), new TypeBasedRegistryKey(typeof(string)), true },
            };
        }
    }
}
