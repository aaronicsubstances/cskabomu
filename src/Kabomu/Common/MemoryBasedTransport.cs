﻿using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class MemoryBasedTransport : IQuasiHttpTransport
    {
        private readonly Random _randGen = new Random();

        public MemoryBasedTransportHub Hub { get; set; }
        public double DirectSendRequestProcessingProbability { get; set; }

        public int MaxChunkSize { get; set; } = 8_192;

        public bool DirectSendRequestProcessingEnabled => _randGen.NextDouble() < DirectSendRequestProcessingProbability;

        public IMutexApi Mutex { get; set; }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
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

        public void OnReleaseConnection(object connection)
        {
            Mutex.RunExclusively(_ =>
            {
                var typedConnection = connection as MemoryBasedTransportConnection;
                typedConnection?.Release(Mutex);
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
                    typedConnection.ProcessReadRequest(Mutex, this, data, offset, length, cb);
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
                    if (!typedConnection.IsConnectionEstablished)
                    {
                        var remoteClient = Hub.Clients[typedConnection.RemoteEndpoint];
                        typedConnection.MarkConnectionAsEstablished(this);
                        remoteClient.OnReceive(connection);
                    }
                    typedConnection.ProcessWriteRequest(Mutex, this, data, offset, length, cb);
                }
                catch (Exception e)
                {
                    cb.Invoke(e);
                }
            }, null);
        }
    }
}
