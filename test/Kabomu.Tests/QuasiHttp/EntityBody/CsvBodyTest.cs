using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class CsvBodyTest
    {
        [Fact]
        public async Task TestReading1()
        {
            // arrange
            var srcData = new Dictionary<string, IList<string>>();
            string expected = "";
            var instance = new CsvBody(srcData);

            // act and assert
            await IOUtilsTest.TestReading(instance.Reader(), null, -1, expected, null);
            Assert.Equal(-1, instance.ContentLength);
        }

        [Fact]
        public async Task TestReading2()
        {
            // arrange
            var srcData = new Dictionary<string, IList<string>>()
            {
                { "A", new List<string> {"b", "2"} },
                { "B", new List<string> { "2"} },
                { "C", new List<string>() },
                { "D", new List<string>{ "Fire" } }
            };
            string expected = "A,b,2\nB,2\nC\nD,Fire\n";
            var instance = new CsvBody(srcData);
            var writer = new DemoCustomReaderWriter();

            // act and assert
            await IOUtilsTest.TestReading(instance.Reader(), writer, 0, expected,
                _ => ByteUtils.BytesToString(writer.BufferStream.ToArray()));
            Assert.Equal(-1, instance.ContentLength);
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var expected = ByteUtils.StringToBytes("c,2\n");
            var srcData = new Dictionary<string, IList<string>>
            {
                { "c", new List<string> { "2"} },
            };
            var instance = new CsvBody(srcData);

            Assert.Equal(-1, instance.ContentLength);

            // verify custom dispose is a no-op
            await instance.Release();

            var reader = instance.Reader();

            var actual = new byte[3];
            var actualLen = await reader.ReadBytes(actual, 0, 3);
            Assert.Equal(3, actualLen);
            ComparisonUtils.CompareData(expected, 0, actualLen,
                actual, 0, actualLen);

            // verify custom dispose is a no-op
            await instance.Release();

            actualLen = await reader.ReadBytes(actual, 1, 2);
            Assert.Equal(1, actualLen);
            ComparisonUtils.CompareData(expected, 3, actualLen,
                actual, 1, actualLen);

            await reader.CustomDispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadBytes(actual, 0, actual.Length));
        }

        [Fact]
        public async Task TestWriting1()
        {
            // arrange
            var srcData = new Dictionary<string, IList<string>>();
            string expected = "";
            var instance = new CsvBody(srcData);
            var writer = new DemoCustomReaderWriter();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.BufferStream.ToArray()));
        }

        [Fact]
        public async Task TestWriting2()
        {
            // arrange
            var srcData = new Dictionary<string, IList<string>>()
            {
                { "A", new List<string> {"b", "2"} },
                { "B", new List<string> { "2"} },
                { "C", new List<string>() },
                { "D", new List<string>{ "Fire" } }
            };
            string expected = "A,b,2\nB,2\nC\nD,Fire\n";
            var instance = new CsvBody(srcData);
            var writer = new DemoCustomReaderWriter();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.BufferStream.ToArray()));
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new CsvBody(null);
            });
        }
    }
}
