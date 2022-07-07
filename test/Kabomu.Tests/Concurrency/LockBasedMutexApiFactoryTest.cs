using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Concurrency
{
    public class LockBasedMutexApiFactoryTest
    {
        [Fact]
        public async Task TestCreate()
        {
            var instance = new LockBasedMutexApiFactory();
            var mutex = await instance.Create();
            Assert.True(mutex is LockBasedMutexApi);
        }
    }
}
