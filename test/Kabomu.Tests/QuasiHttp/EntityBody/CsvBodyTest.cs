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
    public class CsvBodyTest
    {
        [Fact]
        public async Task TestReading1()
        {
            // arrange
            var srcData = new Dictionary<string, IList<string>>();
            var expected = "";
            var instance = new CsvBody(srcData);
            Assert.Equal(-1, instance.ContentLength);

            // act
            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));

            // assert
            Assert.Equal(expected, actual);
            Assert.Equal(-1, instance.ContentLength);

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(expected, actual);
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
            Assert.Equal(-1, instance.ContentLength);

            // act
            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));

            // assert
            Assert.Equal(expected, actual);
            Assert.Equal(-1, instance.ContentLength);

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriting1()
        {
            // arrange
            var srcData = new Dictionary<string, IList<string>>();
            string expected = "";
            var instance = new CsvBody(srcData);
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
