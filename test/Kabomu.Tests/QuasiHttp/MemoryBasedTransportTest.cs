using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    /// <summary>
    /// Decided to have this test in this project instead of in
    /// integration tests, so that the integration tests can have
    /// exclusive access to log files.
    /// </summary>
    public class MemoryBasedTransportTest
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Fact]
        public async Task TestOperations()
        {
            IConnectionAllocationResponse latestConnectionAtAccra = null,
                latestConnectionAtKumasi = null;
            string accraEndpoint = "accra", kumasiEndpoint = "kumasi";
            var accraBasedServer = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    latestConnectionAtAccra = c;
                }
            };
            var kumasiBasedServer = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    latestConnectionAtKumasi = c;
                }
            };
            var instanceA = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { kumasiEndpoint, kumasiBasedServer }
                }
            };
            var instanceK = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { accraEndpoint, accraBasedServer }
                }
            };
            var randGen = new Random();
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                if (randGen.Next(2) == 1)
                {
                    var clientC = await instanceA.AllocateConnection(kumasiEndpoint, null);
                    Assert.Same(clientC, latestConnectionAtKumasi);
                    tasks.Add(PerformProcessing(instanceA,
                        clientC.Connection, true, true));
                    tasks.Add(PerformProcessing(kumasiBasedServer,
                        clientC.Connection, false, false));
                }
                else
                {
                    var clientC = await instanceK.AllocateConnection(
                        accraEndpoint, null);
                    Assert.Same(clientC, latestConnectionAtAccra);
                    tasks.Add(PerformProcessing(instanceK,
                        clientC.Connection, true, false));
                    tasks.Add(PerformProcessing(accraBasedServer,
                        clientC.Connection, false, true));
                }
            }
            await Task.WhenAll(tasks);
        }

        private async Task PerformProcessing(IQuasiHttpTransport transport,
            object connection, bool isClient, bool isAtAccra)
        {
            var transportReader = transport.GetReader(connection);
            var transportWriter = transport.GetWriter(connection);
            var readerWriter = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    IOUtils.ReadBytes(transportReader, data, offset, length),
                WriteFunc = (data, offset, length) =>
                    IOUtils.WriteBytes(transportWriter, data, offset, length)
            };

            var randGen = new Random();

            // ensure at least one question to answer
            var maxQuestionsToAnswer = randGen.Next(10) + 1;
            LogMsg(DescribeMaxQuestionsToAnswer(connection, maxQuestionsToAnswer,
                isClient, isAtAccra));
            bool peerCannotAnswerQuestions = false;
            var numOfQuestionsAnswered = 0;

            // ensure client asks question first time,
            // and server waits to answer question the first time.
            int localPriority = isClient ? 1 : 0;
            int peerPriority = isClient ? 0 : 1;

            int localCalculationResult = isAtAccra ? 1 : 0;

            while (numOfQuestionsAnswered < maxQuestionsToAnswer ||
                !peerCannotAnswerQuestions)
            {
                // determine whether question is to be asked or answered.
                bool ask;
                if (peerCannotAnswerQuestions)
                {
                    ask = false;
                }
                else if (numOfQuestionsAnswered >= maxQuestionsToAnswer)
                {
                    ask = true;
                }
                else
                {
                    if (isClient)
                    {
                        ask = localPriority >= peerPriority;
                    }
                    else
                    {
                        ask = localPriority > peerPriority;
                    }
                }
                if (ask)
                {
                    localPriority = randGen.Next();
                    var outgoingQuestion = new TestMessage
                    {
                        Input = randGen.Next(),
                        Priority = localPriority,
                        CannotAnswerQuestions = numOfQuestionsAnswered >= maxQuestionsToAnswer
                    };
                    LogMsg(DescribeMsg(connection, outgoingQuestion, true, true, isClient, isAtAccra, 0, 0));
                    await WriteMsg(readerWriter, outgoingQuestion);
                    var incomingAnswer = await ReadMsg(readerWriter);
                    LogMsg(DescribeMsg(connection, incomingAnswer, false, false, isClient, isAtAccra, 0, 0));
                    // check answer.
                    Assert.Equal(outgoingQuestion.Input % 2 != localCalculationResult,
                        incomingAnswer.Output);
                    peerPriority = incomingAnswer.Priority;
                    peerCannotAnswerQuestions = incomingAnswer.CannotAnswerQuestions;
                }
                else
                {
                    var incomingQuestion = await ReadMsg(readerWriter);
                    LogMsg(DescribeMsg(connection, incomingQuestion, true, false, isClient, isAtAccra,
                        numOfQuestionsAnswered, maxQuestionsToAnswer));
                    peerPriority = incomingQuestion.Priority;
                    peerCannotAnswerQuestions = incomingQuestion.CannotAnswerQuestions;
                    // answer question
                    ++numOfQuestionsAnswered;
                    localPriority = randGen.Next();
                    var outgoingAnswer = new TestMessage
                    {
                        Input = incomingQuestion.Input,
                        Output = incomingQuestion.Input % 2 == localCalculationResult,
                        Priority = localPriority,
                        CannotAnswerQuestions = numOfQuestionsAnswered >= maxQuestionsToAnswer
                    };
                    LogMsg(DescribeMsg(connection, outgoingAnswer, false, true, isClient, isAtAccra,
                        numOfQuestionsAnswered - 1, maxQuestionsToAnswer));
                    await WriteMsg(readerWriter, outgoingAnswer);
                }
            }
            LogMsg(DescribeRelease(connection, isClient, isAtAccra));
        }

        private static void LogMsg(string msg)
        {
            Log.Info(msg);
        }

        private static string DescribeMaxQuestionsToAnswer(object connection,
            int maxQuestionsToAnswer, bool isClient, bool isAtAccra)
        {
            var part1 = isClient ? "client" : "server";
            var part2 = isAtAccra ? "Accra" : "Kumasi";
            return $"#{connection.GetHashCode()}# maxQuestionsToAnswer = " +
                $"{maxQuestionsToAnswer} at {part1} in {part2}";
        }

        private static string DescribeRelease(object connection,
            bool isClient, bool isAtAccra)
        {
            var part1 = isClient ? "client" : "server";
            var part2 = isAtAccra ? "Accra" : "Kumasi";
            return $"#{connection.GetHashCode()}# release connection at {part1} in {part2}";
        }

        private static string DescribeMsg(object connection, TestMessage msg,
            bool isQuestion, bool isOutgoing, bool isClient, bool isAtAccra,
            int num, int maxNum)
        {
            var part0 = "";
            if (maxNum > 0)
            {
                part0 = $"{num + 1}/{maxNum} ";
            }
            var part1 = isQuestion ? "question" : "answer";
            var part2 = isOutgoing ? "to be sent from" : "received at";
            var part3 = isClient ? "client" : "server";
            var part4 = isAtAccra ? "Accra" : "Kumasi";
            string part5;
            if (isAtAccra)
            {
                if (isQuestion)
                {
                    part5 = isOutgoing ? "even" : "odd";
                }
                else
                {
                    part5 = isOutgoing ? "odd" : "even";
                }
            }
            else
            {
                if (isQuestion)
                {
                    part5 = isOutgoing ? "odd" : "even";
                }
                else
                {
                    part5 = isOutgoing ? "even" : "odd";
                }
            }
            var part6 = "";
            if (!isQuestion)
            {
                part6 = msg.Output ? " Yes" : " No";
            }
            return $"#{connection.GetHashCode()}# " +
                $"{part0}{part1} {part2} {part3} in {part4}: is " +
                $"{msg.Input} {part5}?{part6}\n" +
                $"(Input={msg.Input},Output={msg.Output},Priority={msg.Priority},CAQ={msg.CannotAnswerQuestions})";
        }

        private static async Task<TestMessage> ReadMsg(ICustomReader reader)
        {
            var msgBytes = new byte[10];
            await IOUtils.ReadBytesFully(reader, msgBytes, 0, msgBytes.Length);
            var msg = new TestMessage();
            msg.Input = (int)ByteUtils.DeserializeUpToInt64BigEndian(msgBytes, 0, 4, true);
            msg.Priority = (int)ByteUtils.DeserializeUpToInt64BigEndian(msgBytes, 4, 4, true);
            msg.CannotAnswerQuestions = msgBytes[8] != 0;
            msg.Output = msgBytes[9] != 0;
            return msg;
        }

        private static async Task WriteMsg(ICustomWriter writer,
            TestMessage msg)
        {
            var msgBytes = new byte[10];
            ByteUtils.SerializeUpToInt64BigEndian(msg.Input, msgBytes, 0, 4);
            ByteUtils.SerializeUpToInt64BigEndian(msg.Priority, msgBytes, 4, 4);
            msgBytes[8] = msg.CannotAnswerQuestions ? (byte)1 : (byte)0;
            msgBytes[9] = msg.Output ? (byte)1 : (byte)0;
            await writer.WriteBytes(msgBytes, 0, msgBytes.Length);
        }

        class TestMessage
        {
            public int Input { get; set; }
            public bool Output { get; set; }
            public int Priority { get; set; }
            public bool CannotAnswerQuestions { get; set; }
        }
    }
}
