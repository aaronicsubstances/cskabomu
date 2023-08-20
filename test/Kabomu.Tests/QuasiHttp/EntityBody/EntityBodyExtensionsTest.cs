using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class EntityBodyExtensionsTest
    {
        [Fact]
        public void TestAsReaderForErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
                EntityBodyExtensions.AsReader(null));
        }

        [Theory]
        [MemberData(nameof(CreateTestAsReaderData))]
        public async Task TestAsReader(object reader,
            ICustomWritable fallback, byte[] expected)
        {
            var body = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => reader,
                Writable = fallback,
            };
            byte[] actual = null;
            var desired = IOUtils.ReadAllBytes(body.AsReader());
            // just in case error causes desired to hang forever,
            // impose timeout
            var first = await Task.WhenAny(Task.Delay(3000),
                desired);
            if (first == desired)
            {
                actual = await desired;
            }
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestAsReaderData()
        {
            var testData = new List<object[]>();

            var expected = new byte[0];
            object reader = new MemoryStream();
            ICustomWritable fallback = null;
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2, 3 };
            reader = new MemoryStream(expected);
            fallback = new LambdaBasedCustomWritable();
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2 };
            reader = null;
            fallback = new LambdaBasedCustomWritable
            {
                WritableFunc = writer =>
                {
                    return IOUtils.WriteBytes(writer, expected, 0,
                        expected.Length);
                }
            };
            testData.Add(new object[] { reader, fallback, expected });

            return testData;
        }

        [Fact]
        public async Task TestAsReaderForErrorOnWritable()
        {
            var troublesomeWritable = new LambdaBasedCustomWritable
            {
                WritableFunc = async (writer) =>
                {
                    await IOUtils.WriteBytes(writer, new byte[1000], 0, 1000);
                    throw new Exception("enough!");
 
                }
            };
            var body = new LambdaBasedQuasiHttpBody
            {
                Writable = troublesomeWritable
            };
            Exception actualEx = null;
            var desired = Assert.ThrowsAsync<Exception>(() =>
                IOUtils.ReadAllBytes(body.AsReader()));

            // just in case error causes desired to hang forever,
            // impose timeout
            var first = await Task.WhenAny(Task.Delay(3000),
                desired);
            if (first == desired)
            {
                actualEx = await desired;
            }
            Assert.Equal("enough!", actualEx?.Message);
        }
    }
}
