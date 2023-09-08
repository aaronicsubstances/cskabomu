using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class DemoQuasiHttpTransport : IQuasiHttpTransport
    {
        private readonly object _expectedConnection;
        private readonly object _backingReader;
        private readonly object _backingWriter;
        private int _releaseCallCount;

        public DemoQuasiHttpTransport(object expectedConnection,
            object backingReader, object backingWriter)
        {
            _expectedConnection = expectedConnection;
            _backingReader = backingReader;
            _backingWriter = backingWriter;
        }

        public int ReleaseCallCount => _releaseCallCount;

        public object GetWriter(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            if (_backingWriter == null)
            {
                return null;
            }
            return new LambdaBasedCustomReaderWriter
            {
                WriteFunc = async (data, offset, length) =>
                {
                    await Task.Yield();
                    await IOUtils.WriteBytes(_backingWriter, data, offset, length);
                }
            };
        }

        public object GetReader(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            if (_backingReader == null)
            {
                return null;
            }
            return new LambdaBasedCustomReaderWriter
            {
                ReadFunc = async (data, offset, length) =>
                {
                    await Task.Yield();
                    return await IOUtils.ReadBytes(_backingReader, data, offset, length);
                }
            };
        }

        public async Task ReleaseConnection(object connection)
        {
            if (connection != _expectedConnection)
            {
                throw new ArgumentException("unexpected connection");
            }
            Interlocked.Increment(ref _releaseCallCount);
            await Task.Yield();
        }
    }
}
