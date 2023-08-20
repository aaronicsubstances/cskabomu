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
            var srcData = new Dictionary<string, IList<string>>();
            var expected = "";
            var instance = new CsvBody(srcData);
            Assert.Equal(-1, instance.ContentLength);

            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(expected, actual);

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

            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(expected, actual);

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
            var srcData = new Dictionary<string, IList<string>>();
            string expected = "";
            var instance = new CsvBody(srcData);
            Assert.Equal(-1, instance.ContentLength);

            var writer = new MemoryStream();

            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            writer.SetLength(0); // reset
            instance.ContentLength = -1; // should have no effect on expectations
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));
        }

        [Fact]
        public async Task TestWriting2()
        {
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

            var writer = new MemoryStream();

            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            writer.SetLength(0); // reset
            instance.ContentLength = -1; // should have no effect on expectations
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
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
