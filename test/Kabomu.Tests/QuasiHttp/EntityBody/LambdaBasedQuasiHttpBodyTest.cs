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
        public static List<object[]> CreateTestData()
        {
            return new List<object[]>
            {
                new object[]{ "" },
                new object[]{ "1" },
                new object[]{ "ab" },
                new object[]{ "\u0019\u0020\u0021" },
                new object[]{ "abcd"},
                new object[]{ "\u0150\u0151\u0169\u0172\u0280"}
            };
        }

        [MemberData(nameof(CreateTestData))]
        [Theory]
        public async Task TestReading(string srcData)
        {
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

            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(srcData, actual);

            // verify that release is a no-op
            await instance.Release();

            // assert non-repeatability.
            actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal("", actual);
        }

        [MemberData(nameof(CreateTestData))]
        [Theory]
        public async Task TestReadingAndRelease(string srcData)
        {
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

            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(srcData, actual);

            // verify that release kicks in
            await instance.Release();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                IOUtils.ReadAllBytes(instance.Reader));
        }

        [MemberData(nameof(CreateTestData))]
        [Theory]
        public async Task TestWriting(string expected)
        {
            var instance = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => new MemoryStream(
                    ByteUtils.StringToBytes(expected))
            };
            Assert.Equal(-1, instance.ContentLength);

            var writer = new MemoryStream();

            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            writer.SetLength(0); // reset
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));
        }

        [MemberData(nameof(CreateTestData))]
        [Theory]
        public async Task TestWritingAndRelease(string expected)
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

            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release kicks in
            await instance.Release();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                instance.WriteBytesTo(writer));
        }

        [Fact]
        public async Task TestDelegateWritable()
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

            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert continuation.
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected + expected, ByteUtils.BytesToString(
                writer.ToArray()));
        }

        [Fact]
        public async void TestForErrors()
        {
            var instance = new LambdaBasedQuasiHttpBody();
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.WriteBytesTo(new MemoryStream()));
        }
    }
}
