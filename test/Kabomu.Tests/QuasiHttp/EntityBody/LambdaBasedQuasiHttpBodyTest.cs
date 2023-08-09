using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class LambdaBasedQuasiHttpBodyTest
    {
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("abcd")]
        [InlineData("abcde")]
        [InlineData("abcdef")]
        [InlineData("Foo \u00c0\u00ff")]
        [Theory]
        public async Task TestReading1(string srcData)
        {
            // arrange
            var stream = new MemoryStream(ByteUtils.StringToBytes(srcData));
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    stream.ReadAsync(data, offset, length)
            };
            var instance = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => reader
            };
            Assert.Equal(-1, instance.ContentLength);

            // act
            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));

            // assert
            Assert.Equal(srcData, actual);
            Assert.Equal(-1, instance.ContentLength);

            // verify that release is a no-op
            await instance.Release();

            // assert non-repeatability.
            actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal("", actual);
        }

        [InlineData("")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("abcd")]
        [InlineData("abcde")]
        [InlineData("abcdef")]
        [InlineData("Foo \u00c0\u00ff")]
        [Theory]
        public async Task TestReading2(string srcData)
        {
            // arrange
            var stream = new MemoryStream(ByteUtils.StringToBytes(srcData));
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    stream.ReadAsync(data, offset, length)
            };
            var instance = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => reader,
                ReleaseFunc = async () =>
                {
                    await stream.DisposeAsync();
                }
            };
            Assert.Equal(-1, instance.ContentLength);

            // act
            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));

            // assert
            Assert.Equal(srcData, actual);
            Assert.Equal(-1, instance.ContentLength);

            // verify that release kicks in
            await instance.Release();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                IOUtils.ReadAllBytes(instance.Reader));
        }

        [Theory]
        [InlineData("")]
        [InlineData("d")]
        [InlineData("ds")]
        [InlineData("data")]
        [InlineData("datadriven")]
        [InlineData("Foo \u00c0\u00ff")]
        public async Task TestWriting1(string expected)
        {
            var instance = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => new MemoryStream(
                    ByteUtils.StringToBytes(expected))
            };
            Assert.Equal(-1, instance.ContentLength);
            var writer = new MemoryStream();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected + expected, ByteUtils.BytesToString(
                writer.ToArray()));
        }

        [Theory]
        [InlineData("")]
        [InlineData("d")]
        [InlineData("ds")]
        [InlineData("data")]
        [InlineData("datadriven")]
        [InlineData("Foo \u00c0\u00ff")]
        public async Task TestWriting2(string expected)
        {
            var stream = new MemoryStream(ByteUtils.StringToBytes(expected));
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    stream.ReadAsync(data, offset, length)
            };
            var instance = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => reader,
                ReleaseFunc = async () =>
                {
                    await stream.DisposeAsync();
                }
            };
            Assert.Equal(-1, instance.ContentLength);
            var writer = new MemoryStream();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release kicks in
            await instance.Release();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                instance.WriteBytesTo(writer));
        }

        [Fact]
        public async Task TestWriting3()
        {
            var expected = "sea";
            var srcData = ByteUtils.StringToBytes(expected);
            var writable = new LambdaBasedCustomWritable
            {
                WritableFunc = (writer) =>
                    IOUtils.WriteBytes(writer, srcData, 0, srcData.Length)
            };
            var instance = new LambdaBasedQuasiHttpBody
            {
                Writable = writable
            };
            Assert.Equal(-1, instance.ContentLength);
            var writer = new MemoryStream();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected + expected, ByteUtils.BytesToString(
                writer.ToArray()));
        }
    }
}
