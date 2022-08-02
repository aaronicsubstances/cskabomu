using Kabomu.QuasiHttp.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Server
{
    public class StandardQuasiHttpServerTest
    {
        [Fact]
        public async Task TestProcessReceiveForArgumentErrors()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                var instance = new StandardQuasiHttpServer();
                return instance.ProcessReceiveRequest(null, null);
            });
        }
    }
}
