using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class MemoryBasedTransport : IQuasiHttpTransport
    {
        public object LocalEndpoint { get; set; }
        public MemoryBasedTransportHub Hub { get; set; }

        public int MaxChunkSize { get; set; }

        public bool DirectSendRequestProcessingEnabled { get; set; }

        public IMutexApi Mutex { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            Mutex.RunExclusively(_ =>
            {
                try
                {
                    var remoteClient = Hub.Clients[remoteEndpoint];
                    remoteClient.Application.ProcessRequest(request, cb);
                }
                catch (Exception e)
                {
                    cb.Invoke(e, null);
                }
            }, null);
        }

        public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            Mutex.RunExclusively(_ =>
            {
                try
                {
                    var connection = new MemoryBasedTransportConnection(remoteEndpoint);
                    cb.Invoke(null, connection);
                }
                catch (Exception e)
                {
                    cb.Invoke(e, null);
                }
            }, null);
        }

        public void ReleaseConnection(object connection)
        {
            Mutex.RunExclusively(_ =>
            {
                var typedConnection = (MemoryBasedTransportConnection)connection;
                typedConnection?.Release();
            }, null);
        }

        public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var typedConnection = (MemoryBasedTransportConnection)connection;
            if (typedConnection == null)
            {
                throw new ArgumentException("null connection");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid payload");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            Mutex.RunExclusively(_ =>
            {
                try
                {
                    typedConnection.ProcessReadRequest(LocalEndpoint, data, offset, length, cb);
                }
                catch (Exception e)
                {
                    cb.Invoke(e, 0);
                }
            }, null);
        }

        public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var typedConnection = (MemoryBasedTransportConnection)connection;
            if (typedConnection == null)
            {
                throw new ArgumentException("null connection");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid payload");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            Mutex.RunExclusively(_ =>
            {
                try
                {
                    typedConnection.ProcessWriteRequest(LocalEndpoint, data, offset, length, cb);
                    if (!typedConnection.IsConnectionEstablished)
                    {
                        var remoteClient = Hub.Clients[typedConnection.RemoteEndpoint];
                        typedConnection.MarkConnectionAsEstablished();
                        remoteClient.OnReceive(connection);
                    }
                }
                catch (Exception e)
                {
                    cb.Invoke(e);
                }
            }, null);
        }
    }
}
