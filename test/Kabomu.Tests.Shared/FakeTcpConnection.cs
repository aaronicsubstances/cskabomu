using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class FakeTcpConnection
    {
        public object _remoteEndpoint;
        private readonly Dictionary<FakeTcpTransport, MemoryStream> _writeStreams;
        private readonly Dictionary<FakeTcpTransport, long> _readPositions;

        public FakeTcpConnection(object remoteEndpoint, FakeTcpTransport initial)
        {
            _remoteEndpoint = remoteEndpoint;
            _writeStreams = new Dictionary<FakeTcpTransport, MemoryStream>();
            _writeStreams.Add(initial, new MemoryStream());
            _readPositions = new Dictionary<FakeTcpTransport, long>();
            _readPositions.Add(initial, 0);
        }

        public bool ConnectionEstablished => _remoteEndpoint == null;

        public void EstablishConnection(FakeTcpTransport peer)
        {
            _remoteEndpoint = null;
            _writeStreams.Add(peer, new MemoryStream());
            _readPositions.Add(peer, 0);
        }

        public MemoryStream GetWriteStream(FakeTcpTransport connectedTransport)
        {
            return _writeStreams[connectedTransport];
        }

        public MemoryStream GetReadStream(FakeTcpTransport connectedTransport)
        {
            FakeTcpTransport otherConnectedTransport = null;
            foreach (var key in _writeStreams.Keys)
            {
                if (key != connectedTransport)
                {
                    otherConnectedTransport = key;
                    break;
                }
            }
            return _writeStreams[otherConnectedTransport];
        }

        public long GetReadStreamPosition(FakeTcpTransport connectedTransport)
        {
            return _readPositions[connectedTransport];
        }

        public void IncrementReadPosition(FakeTcpTransport connectedTransport, int bytesRead)
        {
            _readPositions[connectedTransport] += bytesRead;
        }
    }
}
