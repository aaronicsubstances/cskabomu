﻿using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class LeadChunkTest
    {
        [Fact]
        public void TestRecoveryWithDefaultValues()
        {
            var expected = new LeadChunk();
            var serialized = expected.Serialize();
            var inputStream = new MemoryStream();
            foreach (var item in serialized)
            {
                inputStream.Write(item.Data, item.Offset, item.Length);
            }
            var bytes = inputStream.ToArray();
            var actual = LeadChunk.Deserialize(bytes, 0, bytes.Length);
            CompareChunks(expected, actual);
        }

        [Fact]
        public void TestRecoveryForNonDefaultValues()
        {
            var expected = new LeadChunk();
            expected.Version = LeadChunk.Version01;
            expected.Flags = 1;
            expected.Path = "/detail";
            expected.StatusIndicatesSuccess = true;
            expected.StatusMessage = "ok";
            expected.HasContent = true;
            expected.ContentType = "text/plain";
            expected.Headers = new Dictionary<string, List<string>>();
            expected.Headers.Add("accept", new List<string> { "text/plain", "text/xml" });
            expected.Headers.Add("a", new List<string>());

            var serialized = expected.Serialize(); ;
            var inputStream = new MemoryStream();
            foreach (var item in serialized)
            {
                inputStream.Write(item.Data, item.Offset, item.Length);
            }
            var bytes = inputStream.ToArray();
            var actual = LeadChunk.Deserialize(bytes, 0, bytes.Length);
            CompareChunks(expected, actual);
        }

        [Fact]
        public void TestForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                LeadChunk.Deserialize(new byte[6], 0, 6);
            });
            Assert.ThrowsAny<Exception>(() =>
            {
                LeadChunk.Deserialize(new byte[7], 0, 7);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                LeadChunk.Deserialize(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 9 }, 0, 11);
            });
        }

        internal static void CompareChunks(LeadChunk expected, LeadChunk actual)
        {
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.Flags, actual.Flags);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.StatusIndicatesSuccess, actual.StatusIndicatesSuccess);
            Assert.Equal(expected.StatusIndicatesClientError, actual.StatusIndicatesClientError);
            Assert.Equal(expected.StatusMessage, actual.StatusMessage);
            Assert.Equal(expected.HasContent, actual.HasContent);
            Assert.Equal(expected.ContentType, actual.ContentType);
            ComparisonUtils.CompareHeaders(expected.Headers, actual.Headers);
        }
    }
}