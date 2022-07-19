﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport, IQuasiHttpTransportBypass
    {
        public MemoryBasedClientTransport()
        {
        }

        public string LocalEndpoint { get; set; }
        public IMemoryBasedTransportHub Hub { get; set; }

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectivityParams connectivityParams)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.ProcessSendRequest(LocalEndpoint, connectivityParams, request);
        }

        public Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.AllocateConnection(LocalEndpoint, connectivityParams);
        }

        public Task ReleaseConnection(object connection)
        {
            return MemoryBasedServerTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.ReadBytesInternal(false, connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.WriteBytesInternal(false, connection, data, offset, length);
        }
    }
}
