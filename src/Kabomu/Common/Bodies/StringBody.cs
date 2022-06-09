using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Bodies
{
    public class StringBody : IQuasiHttpBody
    {
        private readonly SerializableObjectBody _backingBody;

        public StringBody(string content,string contentType)
        {
            _backingBody = new SerializableObjectBody(content,
                SerializeContent, contentType ?? TransportUtils.ContentTypePlainText);
        }

        private static byte[] SerializeContent(object obj)
        {
            var content = (string)obj;
            var dataBytes = Encoding.UTF8.GetBytes(content);
            return dataBytes;
        }

        public string Content => (string)_backingBody.Content;

        public string ContentType => _backingBody.ContentType;

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            _backingBody.ReadBytes(mutex, data, offset, bytesToRead, cb);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            _backingBody.OnEndRead(mutex, e);
        }
    }
}
