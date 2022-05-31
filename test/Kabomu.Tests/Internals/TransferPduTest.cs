using Kabomu.Common;
using Kabomu.Internals;
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

            var bytes = expected.Serialize(false);
            var actual = TransferPdu.Deserialize(bytes, 0, bytes.Length);
            ComparePdus(expected, actual);

            bytes = expected.Serialize(true);
            int encodedLength = ByteUtils.DeserializeInt32BigEndian(bytes, 0);
            Assert.Equal(bytes.Length - 4, encodedLength);
            actual = TransferPdu.Deserialize(bytes, 4, bytes.Length - 4);
            ComparePdus(expected, actual);
        }

        [Fact]
        public void TestRecoveryForNonDefaultValues()
        {
            var expected = new TransferPdu();
            expected.Version = TransferPdu.Version01;
            expected.PduType = TransferPdu.PduTypeRequest;
            expected.Flags = 1;
            expected.SequenceNumber = 19;
            expected.Path = "/detail";
            expected.StatusIndicatesSuccess = true;
            expected.StatusMessage = "ok";
            expected.ContentLength = 34;
            expected.ContentType = "text/plain";
            expected.Headers = new Dictionary<string, List<string>>();
            expected.Headers.Add("accept", new List<string> { "text/plain", "text/xml" });
            expected.Headers.Add("a", new List<string>());

            var bytes = expected.Serialize(false);
            var actual = TransferPdu.Deserialize(bytes, 0, bytes.Length);
            ComparePdus(expected, actual);
            
            bytes = expected.Serialize(true);
            int encodedLength = ByteUtils.DeserializeInt32BigEndian(bytes, 0);
            Assert.Equal(bytes.Length - 4, encodedLength);
            actual = TransferPdu.Deserialize(bytes, 4, bytes.Length - 4);
            ComparePdus(expected, actual);
        }

        [Fact]
        public void TestForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                TransferPdu.Deserialize(new byte[10], 0, 10);
            });
            Assert.ThrowsAny<Exception>(() =>
            {
                TransferPdu.Deserialize(new byte[11], 0, 11);
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
            Assert.Equal(expected.SequenceNumber, actual.SequenceNumber);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.StatusIndicatesSuccess, actual.StatusIndicatesSuccess);
            Assert.Equal(expected.StatusIndicatesClientError, actual.StatusIndicatesClientError);
            Assert.Equal(expected.StatusMessage, actual.StatusMessage);
            Assert.Equal(expected.ContentLength, actual.ContentLength);
            Assert.Equal(expected.ContentType, actual.ContentType);
            CompareHeaders(expected.Headers, actual.Headers);
            CompareData(expected.Data, expected.DataOffset, expected.DataLength, actual.Data,
                actual.DataOffset, actual.DataLength);
        }

        internal static void CompareHeaders(Dictionary<string, List<string>> expected,
            Dictionary<string, List<string>> actual)
        {
            var expectedKeys = new List<string>();
            if (expected != null)
            {
                foreach (var key in expected.Keys)
                {
                    var value = expected[key];
                    if (value != null & value.Count > 0)
                    {
                        expectedKeys.Add(key);
                    }
                }
            }
            expectedKeys.Sort();
            var actualKeys = new List<string>();
            if (actual != null)
            {
                foreach (var key in actual.Keys)
                {
                    var value = actual[key];
                    if (value != null & value.Count > 0)
                    {
                        actualKeys.Add(key);
                    }
                }
            }
            actualKeys.Sort();
            Assert.Equal(expectedKeys, actualKeys);
            foreach (var key in expectedKeys)
            {
                var expectedValue = expected[key];
                var actualValue = actual[key];
                Assert.Equal(expectedValue, actualValue);
            }
        }

        private static void CompareData(byte[] expectedData, int expectedDataOffset, int expectedDataLength,
            byte[] actualData, int actualDataOffset, int actualDataLength)
        {
            Assert.Equal(expectedDataLength, actualDataLength);
            for (int i = 0; i < expectedDataLength; i++)
            {
                var expectedByte = expectedData[expectedDataOffset + i];
                var actualByte = actualData[actualDataOffset + i];
                Assert.Equal(expectedByte, actualByte);
            }
        }
    }
}
