using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
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
            await IOUtilsTest.TestReading(instance, null, -1, expected, null);
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
            var writer = new DemoSimpleCustomWriter();

            // act and assert
            await IOUtilsTest.TestReading(instance, writer, 0, expected,
                _ => writer.Buffer.ToString());
            Assert.Equal(-1, instance.ContentLength);
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var expected = Encoding.UTF8.GetBytes("c,2\n");
            var srcData = new Dictionary<string, IList<string>>
            {
                { "c", new List<string> { "2"} },
            };
            var instance = new CsvBody(srcData);

            Assert.Equal(-1, instance.ContentLength);

            // verify custom dispose is a no-op
            await instance.CustomDispose();

            var actual = new byte[3];
            var actualLen = await instance.ReadBytes(actual, 0, 3);
            Assert.Equal(3, actualLen);
            ComparisonUtils.CompareData(expected, 0, actualLen,
                actual, 0, actualLen);

            // verify custom dispose is a no-op
            await instance.CustomDispose();

            actualLen = await instance.ReadBytes(actual, 1, 2);
            Assert.Equal(1, actualLen);
            ComparisonUtils.CompareData(expected, 3, actualLen,
                actual, 1, actualLen);
        }

        [Fact]
        public async Task TestWriting1()
        {
            // arrange
            var srcData = new Dictionary<string, IList<string>>();
            string expected = "";
            var instance = new CsvBody(srcData);
            var writer = new DemoSimpleCustomWriter();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, writer.Buffer.ToString());
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
            var writer = new DemoSimpleCustomWriter();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, writer.Buffer.ToString());
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
