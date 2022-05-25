using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class ByteBufferBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[0], 0, 0, "text/plain");
            var mutex = new TestEventLoopApi();
            var buffer = new byte[10];

            // act and assert.
            Assert.Equal("text/plain", instance.ContentType);
            Assert.Equal(0, instance.ContentLength);

            Action<Exception, int> cb1 = (e, bytesRead) =>
            {
                Assert.Null(e);
                Assert.Equal(0, bytesRead);
            };
            instance.OnDataRead(mutex, buffer, 5, 5, cb1);

            Action<Exception, int> cb2 = (e, bytesRead) =>
            {
                Assert.Null(e);
                Assert.Equal(0, bytesRead);
            };
            instance.OnDataRead(mutex, buffer, 0, buffer.Length, cb2);

            instance.OnEndRead(mutex, new Exception("EOF"));
            instance.OnEndRead(mutex, null);

            Action<Exception, int> endCb = (e, bytesRead) =>
            {
                Assert.NotNull(e);
                Assert.Equal("EOF", e.Message);
            };
            instance.OnDataRead(mutex, buffer, 0, buffer.Length, endCb);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[] { (byte)'A', (byte)'b', (byte)'2' }, 0, 3, null);
            var mutex = new TestEventLoopApi();
            var buffer = new byte[10];
            var stringBuilder = new StringBuilder();

            // act and assert.
            Assert.Equal("application/octet-stream", instance.ContentType);
            Assert.Equal(3, instance.ContentLength);

            Action<Exception, int> cb1 = (e, bytesRead) =>
            {
                Assert.Null(e);
                Assert.Equal(2, bytesRead);
                for (int i = 0; i < bytesRead; i++)
                {
                    stringBuilder.Append((char)buffer[i]);
                }
            };
            instance.OnDataRead(mutex, buffer, 0, 2, cb1);

            Action<Exception, int> cb2 = (e, bytesRead) =>
            {
                Assert.Null(e);
                Assert.Equal(1, bytesRead);
                stringBuilder.Append((char)buffer[5]);
            };
            instance.OnDataRead(mutex, buffer, 5, 5, cb2);

            Action<Exception, int> cb3 = (e, bytesRead) =>
            {
                Assert.Null(e);
                Assert.Equal(0, bytesRead);
            };
            instance.OnDataRead(mutex, buffer, 0, buffer.Length, cb3);
            Assert.Equal("Ab2", stringBuilder.ToString());

            instance.OnEndRead(mutex, null);
            instance.OnEndRead(mutex, new Exception("test"));

            Action<Exception, int> endCb = (e, bytesRead) =>
            {
                Assert.NotNull(e);
                Assert.Equal("end of read", e.Message);
            };
            instance.OnDataRead(mutex, buffer, 0, buffer.Length, endCb);
        }
    }
}
