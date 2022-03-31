using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Components
{
    public class DefaultMessageTransferManagerTest
    {
        [Theory]
        [MemberData(nameof(CreateTestGenerateErrorMessageData))]
        public void TestGenerateErrorMessage(int errorCode, string fallback, string expected)
        {
            var actual = DefaultMessageTransferManager.GenerateErrorMessage(errorCode, fallback);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestGenerateErrorMessageData()
        {
            return new List<object[]>
            {
                new object[]{ -3, null, "-3:N/A" },
                new object[]{ -1, "refresh", "refresh" },
                new object[]{ -1, null, "-1:N/A" },
                new object[]{ 0, null, "0:Reserved" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeGeneral, null, "1:General Error" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeProtocolViolation, null, "2:Protocol Violation" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeCancelled, null, "3:Cancelled" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeReset, null, "4:Reset" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeSendTimeout, null, "5:Send Timeout" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeReceiveTimeout, null, "6:Receive Timeout" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeAbortedBySender, null, "7:Aborted by Sender" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeAbortedByReceiver, null, "8:Aborted by Receiver" },
                new object[]{ DefaultMessageTransferManager.ErrorCodeMessageIdNotFound, null, "9:Message Id Not Found" },
                new object[]{ 15, null, "15:Reserved" },
                new object[]{ 30, "30:power outage", "30:power outage" },
                new object[]{ 30, null, "30:N/A" }
            };
        }
    }
}
