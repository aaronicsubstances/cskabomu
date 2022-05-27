using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ByteOrientedTransferBody : IQuasiHttpBody
    {
        private int _readContentLength;
        private Exception _srcEndError;

        public ByteOrientedTransferBody(int contentLength, string contentType,
            IQuasiHttpTransport transport, object connection, Action<Exception> completionCallback)
        {
            ContentLength = contentLength;
            ContentType = contentType;
            Transport = transport;
            Connection = connection;
            CompletionCallback = completionCallback;
        }

        public string ContentType { get; }
        public int ContentLength { get; }
        public IQuasiHttpTransport Transport { get; }
        public object Connection { get; }
        public Action<Exception> CompletionCallback { get; }


        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (bytesToRead < 0)
            {
                throw new ArgumentException("received negative bytes to read");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                Action<Exception, int> wrapperCb = (e, bytesRead) =>
                {
                    if (e != null)
                    {
                        cb.Invoke(e, 0);
                        return;
                    }
                    mutex.RunExclusively(_ =>
                    {
                        if (_srcEndError != null)
                        {
                            cb.Invoke(_srcEndError, 0);
                            return;
                        }
                        if (ContentLength < 0)
                        {
                            cb.Invoke(null, bytesRead);
                            if (bytesRead <= 0)
                            {
                                EndRead(null);
                                return;
                            }
                        }
                        else
                        {
                            if (bytesRead <= 0)
                            {
                                Exception e2 = null;
                                if (_readContentLength != ContentLength)
                                {
                                    e2 = new Exception("expected more content");
                                }
                                cb.Invoke(e2, bytesRead);
                                EndRead(e2);
                                return;
                            }

                            if (_readContentLength + bytesRead > ContentLength)
                            {
                                var e2 = new Exception("content length exceeded");
                                cb.Invoke(e2, 0);
                                EndRead(e2);
                                return;
                            }

                            _readContentLength += bytesRead;
                            cb.Invoke(null, bytesRead); 
                            if (_readContentLength == ContentLength)
                            {
                                EndRead(null);
                                return;
                            }
                        }
                    }, null);
                };
                Transport.ReadBytes(Connection, data, offset, bytesToRead, wrapperCb);
            }, null);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                EndRead(e);
            }, null);
        }
        
        private void EndRead(Exception e)
        {
            CompletionCallback.Invoke(e);
            _srcEndError = e ?? new Exception("end of read");
        }
    }
}
