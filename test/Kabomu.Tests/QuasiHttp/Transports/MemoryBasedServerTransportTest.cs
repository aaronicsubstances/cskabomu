using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Transports
{
    public class MemoryBasedServerTransportTest
    {
        [Fact]
        public async Task Test()
        {
            var instance = new MemoryBasedServerTransport();
            var running = await instance.IsRunning();
            Assert.False(running);
            await instance.Start();
            running = await instance.IsRunning();
            Assert.True(running);
            await instance.Stop();
            running = await instance.IsRunning();
            Assert.False(running);

            await instance.Start();
            await instance.Start();
            running = await instance.IsRunning();
            Assert.True(running);

            

            await instance.Stop();
            await instance.Stop();
            running = await instance.IsRunning();
            Assert.False(running);
        }
    }
}
