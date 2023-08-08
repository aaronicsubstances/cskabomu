using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class EntityBodyExtensionsTest
    {
        [Theory]
        [MemberData(nameof(CreateTestCoalesceAsReaderData))]
        public async Task TestCoalesceAsReader(ICustomReader reader,
            ICustomWritable fallback, byte[] expected)
        {
            reader = IOUtils.CoalesceAsReader(reader, fallback);
            byte[] actual = null;
            if (reader != null)
            {
                var desired = IOUtils.ReadAllBytes(reader, 0, 2);
                // just in case error causes desired to hang forever,
                // impose timeout
                var first = await Task.WhenAny(Task.Delay(3000),
                    desired);
                if (first == desired)
                {
                    actual = await desired;
                }
            }
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestCoalesceAsReaderData()
        {
            var testData = new List<object[]>();

            var expected = new byte[] { };
            var reader = new HelperCustomReaderWritable(expected);
            ICustomWritable fallback = null;
            testData.Add(new object[] { reader, fallback, expected });

            expected = null;
            reader = null;
            fallback = null;
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2, 3 };
            reader = new HelperCustomReaderWritable(expected);
            fallback = new LambdaBasedCustomWritable();
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2, 3, 4, 5 };
            reader = null;
            fallback = new HelperCustomReaderWritable(expected);
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2 };
            reader = null;
            fallback = new LambdaBasedCustomWritable
            {
                WritableFunc = writer =>
                {
                    return writer.WriteBytes(expected, 0, expected.Length);
                }
            };
            testData.Add(new object[] { reader, fallback, expected });

            return testData;
        }
    }
}
