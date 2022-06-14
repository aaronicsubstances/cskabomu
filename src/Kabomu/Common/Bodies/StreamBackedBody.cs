using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class StreamBackedBody : IQuasiHttpBody
    {
        private Exception _srcEndError;

        public StreamBackedBody(Stream backingStream, string contentType)
        {
            if (backingStream == null)
            {
                throw new ArgumentException("null backing stream");
            }
            BackingStream = backingStream;
            ContentType = contentType ?? TransportUtils.ContentTypeByteStream;
        }

        public long ContentLength => -1;

        public string ContentType { get; }

        public Stream BackingStream { get; }

        private Task AcquireExclusiveAccess(IMutexApi mutex)
        {
            var tcs = new TaskCompletionSource<object>();
            mutex.RunExclusively(_ =>
            {
                tcs.SetResult(null);
            }, null);
            return tcs.Task;
        }

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            // this split is for the purpose of getting CLR to bubble up the above validation exceptions,
            // and at the same time be able to use async await in async void methods.
            OnContinueDataRead(mutex, data, offset, bytesToRead, cb);
        }

        private async void OnContinueDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            await AcquireExclusiveAccess(mutex);
            if (_srcEndError != null)
            {
                cb.Invoke(_srcEndError, 0);
                return;
            }
            int bytesRead = 0;
            Exception readError = null;
            try
            {
                bytesRead = await BackingStream.ReadAsync(data, offset, bytesToRead);
            }
            catch (Exception e)
            {
                readError = e;
            }
            await AcquireExclusiveAccess(mutex);
            if (_srcEndError != null)
            {
                cb.Invoke(_srcEndError, 0);
                return;
            }
            if (readError != null)
            {
                await EndRead(cb, readError);
                return;
            }
            cb.Invoke(null, bytesRead);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            // this split is for the purpose of getting CLR to bubble up the above validation exceptions,
            // and at the same time be able to use async await in async void methods.
            OnContinueEndRead(mutex, e);
        }

        private async void OnContinueEndRead(IMutexApi mutex, Exception e)
        {
            await AcquireExclusiveAccess(mutex);
            if (_srcEndError != null)
            {
                return;
            }
            await EndRead(null, e);
        }

        private async Task EndRead(Action<Exception, int> cb, Exception e)
        {
            if (e == null)
            {
                e = new Exception("end of read");
            }
            _srcEndError = e;
            try
            {
                await BackingStream.DisposeAsync();
            }
            catch (Exception)
            {
                // ignore
            }
            // use e rather than _srcEndError to skip need to acquire exclusive access.
            cb?.Invoke(e, 0);
        }
    }
}
