using Kabomu.Common;
using Kabomu.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class TransferPduTest
    {
        [Fact]
        public void TestRecoveryWithDefaultValues()
        {
            var expected = new TransferPdu();
            var bytes = expected.Serialize();
            int encodedLength = ByteUtils.DeserializeInt16BigEndian(bytes, 0);
            Assert.Equal(bytes.Length - 2, encodedLength);
            var actual = TransferPdu.Deserialize(bytes, 2, bytes.Length - 2);
            ComparePdus(expected, actual);
        }

        [Fact]
        public void TestRecoveryForNonDefaultValues()
        {
            var expected = new TransferPdu();
            expected.Version = TransferPdu.Version01;
            expected.PduType = TransferPdu.PduTypeRequest;
            expected.Flags = 1;
            expected.Path = "/detail";
            expected.StatusIndicatesSuccess = true;
            expected.StatusMessage = "ok";
            expected.HasContent = true;
            expected.ContentType = "text/plain";
            expected.Headers = new Dictionary<string, List<string>>();
            expected.Headers.Add("accept", new List<string> { "text/plain", "text/xml" });
            expected.Headers.Add("a", new List<string>());

            var bytes = expected.Serialize();
            int encodedLength = ByteUtils.DeserializeInt16BigEndian(bytes, 0);
            Assert.Equal(bytes.Length - 2, encodedLength);
            var actual = TransferPdu.Deserialize(bytes, 2, bytes.Length - 2);
            ComparePdus(expected, actual);
        }

        [Fact]
        public void TestForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                TransferPdu.Deserialize(new byte[6], 0, 6);
            });
            Assert.ThrowsAny<Exception>(() =>
            {
                TransferPdu.Deserialize(new byte[7], 0, 7);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                TransferPdu.Deserialize(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 9 }, 0, 11);
            });
        }

        internal static void ComparePdus(TransferPdu expected, TransferPdu actual)
        {
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.PduType, actual.PduType);
            Assert.Equal(expected.Flags, actual.Flags);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.StatusIndicatesSuccess, actual.StatusIndicatesSuccess);
            Assert.Equal(expected.StatusIndicatesClientError, actual.StatusIndicatesClientError);
            Assert.Equal(expected.StatusMessage, actual.StatusMessage);
            Assert.Equal(expected.HasContent, actual.HasContent);
            Assert.Equal(expected.ContentType, actual.ContentType);
            ComparisonUtils.CompareHeaders(expected.Headers, actual.Headers);
            ComparisonUtils.CompareData(expected.Data, expected.DataOffset, expected.DataLength, actual.Data,
                actual.DataOffset, actual.DataLength);
        }
    }
}
