using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Client
{
    public class AltSendProtocolInternalTest
    {
        [Fact]
        public async Task TestSend()
        {
            var instance = new AltSendProtocolInternal
            {
                
            };
            var request = new DefaultQuasiHttpRequest
            {

            };
            //var response = await instance.Send(request);
        }
    }
}
