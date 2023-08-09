using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.ChunkedTransfer
{
    public class LeadChunkTest
    {
        internal static async Task<int> Serialize(LeadChunk chunk,
            MemoryStream stream)
        {
            chunk.UpdateSerializedRepresentation();
            int chunkLen = chunk.CalculateSizeInBytesOfSerializedRepresentation();
            await chunk.WriteOutSerializedRepresentation(new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    stream.WriteAsync(data, offset, length)
            });
            return chunkLen;
        }

        [Fact]
        public async Task TestRecoveryWithDefaultValues()
        {
            var expected = new LeadChunk
            {
                Version = LeadChunk.Version01
            };
            var inputStream = new MemoryStream();
            var serializedLen = await Serialize(expected, inputStream);
            var bytes = inputStream.ToArray();
            Assert.Equal(bytes.Length, serializedLen);
            var actual = LeadChunk.Deserialize(bytes, 0, bytes.Length);
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }

        [Fact]
        public async Task TestRecoveryForNonDefaultValues()
        {
            var expected = new LeadChunk();
            expected.Version = LeadChunk.Version01;
            expected.Flags = 1;
            expected.RequestTarget = "/detail";
            expected.HttpStatusMessage = "ok";
            expected.ContentLength = 20;
            expected.StatusCode = 200;
            expected.HttpVersion = "1.1";
            expected.Method = "POST";
            expected.Headers = new Dictionary<string, IList<string>>();
            expected.Headers.Add("accept", new List<string> { "text/plain", "text/xml" });
            expected.Headers.Add("a", new List<string>());
            expected.Headers.Add("b", new List<string> { "myinside\u00c6.team" });

            var inputStream = new MemoryStream();
            var serializedLen = await Serialize(expected, inputStream);
            var bytes = inputStream.ToArray();
            Assert.Equal(bytes.Length, serializedLen);
            var actual = LeadChunk.Deserialize(bytes, 0, bytes.Length);
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }

        [Fact]
        public void TestForErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                LeadChunk.Deserialize(null, 0, 6);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                LeadChunk.Deserialize(new byte[6], 6, 1);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                LeadChunk.Deserialize(new byte[7], 0, 7);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                LeadChunk.Deserialize(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 9 }, 0, 11);
            });
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var data = new byte[] { 0, 0, (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',',(byte)'1', (byte)',',(byte)'1', (byte)',',(byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)'\n' };
                LeadChunk.Deserialize(data, 0, data.Length);
            });
            Assert.Contains("version", ex.Message);
        }
    }
}
