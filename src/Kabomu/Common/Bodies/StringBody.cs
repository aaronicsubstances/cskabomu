using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
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

        public Task<int> ReadBytes(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            return _backingBody.ReadBytes(eventLoop, data, offset, bytesToRead);
        }

        public Task EndRead(IEventLoopApi eventLoop, Exception e)
        {
            return _backingBody.EndRead(eventLoop, e);
        }
    }
}
