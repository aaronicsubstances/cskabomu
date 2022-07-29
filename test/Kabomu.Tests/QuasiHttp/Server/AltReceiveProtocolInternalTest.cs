using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Server
{
    public class AltReceiveProtocolInternalTest
    {
        [Fact]
        public async Task ProcessSendToApplication()
        {
            var instance = new AltReceiveProtocolInternal
            {

            };
            var request = new DefaultQuasiHttpRequest
            {

            };
            //var response = await instance.ProcessSendToApplication(request);
        }
    }
}
