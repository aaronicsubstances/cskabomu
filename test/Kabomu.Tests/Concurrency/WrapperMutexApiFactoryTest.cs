using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Concurrency
{
    public class WrapperMutexApiFactoryTest
    {
        [Fact]
        public async Task TestCreate()
        {
            var expected = new LockBasedMutexApi();
            var instance = new WrapperMutexApiFactory(expected);
            var actual = await instance.Create();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestCreateForErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WrapperMutexApiFactory(null));
        }
    }
}
