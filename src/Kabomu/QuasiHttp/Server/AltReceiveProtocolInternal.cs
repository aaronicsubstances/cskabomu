﻿using Kabomu.Common;
using Kabomu.QuasiHttp.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class AltReceiveProtocolInternal: IReceiveProtocolInternal
    {
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpRequest Request { get; set; }

        public Task Cancel()
        {
            return Task.CompletedTask;
        }

        public async Task<IQuasiHttpResponse> Receive()
        {
            if (Application == null)
            {
                throw new MissingDependencyException("application");
            }
            if (Request == null)
            {
                throw new ExpectationViolationException("request");
            }

            var res = await Application.ProcessRequest(Request);

            if (res == null)
            {
                throw new QuasiHttpRequestProcessingException(
                    QuasiHttpRequestProcessingException.ReasonCodeNoResponse,
                    "null response");
            }

            return res;
        }
    }
}
