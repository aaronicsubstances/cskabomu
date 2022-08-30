using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Registry
{
    public class LazyValueGeneratorInternalTest
    {
        [Fact]
        public void TestErrorsInConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new LazyValueGeneratorInternal<object>(null));
        }

        [Fact]
        public void TestGet()
        {
            var expected = "done";
            var cbCalled = false;
            Func<object> cb = () =>
            {
                if (cbCalled)
                {
                    throw new Exception();
                }
                cbCalled = true;
                return expected;
            };
            var instance = new LazyValueGeneratorInternal<object>(cb);
            Assert.False(cbCalled);
            var actual = instance.Get();
            Assert.True(cbCalled);
            Assert.Equal(expected, actual);

            // test again.
            actual = instance.Get();
            Assert.Equal(expected, actual);
        }
    }
}
