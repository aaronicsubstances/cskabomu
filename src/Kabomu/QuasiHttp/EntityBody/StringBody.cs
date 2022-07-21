using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class StringBody : IQuasiHttpBody
    {
        private readonly SerializableObjectBody _backingBody;

        public StringBody(string content, string contentType)
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
        public long ContentLength => _backingBody.ContentLength;
        public string ContentType => _backingBody.ContentType;

        public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            return _backingBody.ReadBytes(data, offset, bytesToRead);
        }

        public Task EndRead()
        {
            return _backingBody.EndRead();
        }
    }
}
