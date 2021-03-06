using Kabomu.QuasiHttp;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class SubsequentChunkTest
    {
        [Fact]
        public void TestRecoveryWithDefaultValues()
        {
            var expected = new SubsequentChunk
            {
                Version = LeadChunk.Version01
            };
            var serialized = expected.Serialize();
            var inputStream = new MemoryStream();
            foreach (var item in serialized)
            {
                inputStream.Write(item.Data, item.Offset, item.Length);
            }
            var bytes = inputStream.ToArray();
            var actual = SubsequentChunk.Deserialize(bytes, 0, bytes.Length);
            ComparisonUtils.CompareSubsequentChunks(expected, actual);
        }

        [Fact]
        public void TestRecoveryForNonDefaultValues()
        {
            var expected = new SubsequentChunk();
            expected.Version = LeadChunk.Version01;
            expected.Flags = 1;
            expected.Data = new byte[] { 0, (byte)'a', (byte)'b', (byte)'c', 9 };
            expected.DataOffset = 1;
            expected.DataLength = 3;

            var serialized = expected.Serialize(); ;
            var inputStream = new MemoryStream();
            foreach (var item in serialized)
            {
                inputStream.Write(item.Data, item.Offset, item.Length);
            }
            var bytes = inputStream.ToArray();
            var actual = SubsequentChunk.Deserialize(bytes, 0, bytes.Length);
            ComparisonUtils.CompareSubsequentChunks(expected, actual);
        }

        [Fact]
        public void TestForErrors()
        {
             Assert.Throws<ArgumentException>(() =>
            {
                SubsequentChunk.Deserialize(new byte[10], 0, 0);
            });
            Assert.ThrowsAny<Exception>(() =>
            {
                SubsequentChunk.Deserialize(new byte[7], 0, 1);
            });
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                SubsequentChunk.Deserialize(new byte[10], 3, 6);
            });
            Assert.Contains("version", ex.Message);
        }
    }
}
