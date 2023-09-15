using Kabomu.ProtocolImpl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class BodyChunkEncodingWriterTest
    {
        [Fact]
        public void TestAllocateBodyChunkV1HeaderBuffer()
        {
            var actual = BodyChunkEncodingWriter.AllocateBodyChunkV1HeaderBuffer();
            Assert.Equal(13, actual.Length);
        }

        [Theory]
        [MemberData(nameof(CreateTestEncodeBodyChunkV1HeaderData))]
        public void TestEncodeBodyChunkV1Header(int length,
            byte[] sink, int offset, byte[] expected)
        {
            BodyChunkEncodingWriter.EncodeBodyChunkV1Header(length,
                sink, offset);
            Assert.Equal(expected, sink);
        }

        public static List<object[]> CreateTestEncodeBodyChunkV1HeaderData()
        {
            var testData = new List<object[]>();

            int length = 0;
            byte[] sink = new byte[20];
            int offset = 0;
            byte[] expected = new byte[]
            {
                (byte)'0', (byte)'1', (byte)',',
                (byte)'0', (byte)'0', (byte)'0', (byte)'0',
                (byte)'0', (byte)'0', (byte)'0', (byte)'0',
                (byte)'0', (byte)'0',
                0, 0, 0, 0, 0, 0, 0
            };
            testData.Add(new object[] { length, sink, offset, expected });

            length = 2011;
            sink = MiscUtilsInternal.StringToBytes("".PadRight(15, '1'));
            offset = 0;
            expected = new byte[]
            {
                (byte)'0', (byte)'1', (byte)',',
                (byte)'0', (byte)'0', (byte)'0', (byte)'0',
                (byte)'0', (byte)'0', (byte)'2', (byte)'0',
                (byte)'1', (byte)'1',
                (byte)'1', (byte)'1'
            };
            testData.Add(new object[] { length, sink, offset, expected });

            length = 2_011_789_652;
            sink = MiscUtilsInternal.StringToBytes("".PadRight(18, '1') +
                "\r\n");
            offset = 2;
            expected = new byte[]
            {
                (byte)'1', (byte)'1',
                (byte)'0', (byte)'1', (byte)',',
                (byte)'2', (byte)'0', (byte)'1', (byte)'1',
                (byte)'7', (byte)'8', (byte)'9', (byte)'6',
                (byte)'5', (byte)'2',
                (byte)'1', (byte)'1', (byte)'1', (byte)'\r', (byte)'\n'
            };
            testData.Add(new object[] { length, sink, offset, expected });

            return testData;
        }

        [Fact]
        public async Task TestWriteEnd()
        {
            // arrange
            var instance = new BodyChunkEncodingWriter();
            var memStream = new MemoryStream();
            Func<byte[], int, int, Task> sink = (data, offset, length) =>
            {
                memStream.Write(data, offset, length);
                return Task.CompletedTask;
            };
            var expected = "01,0000000000";

            // act
            await instance.WriteEnd(sink);

            // assert
            var actual = MiscUtilsInternal.BytesToString(memStream.ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriteData1()
        {
            // arrange
            var instance = new BodyChunkEncodingWriter();
            var memStream = new MemoryStream();
            Func<byte[], int, int, Task> sink = (data, offset, length) =>
            {
                memStream.Write(data, offset, length);
                return Task.CompletedTask;
            };
            var data = new byte[0];
            var expected = new byte[0];

            // act
            await instance.WriteData(data, sink);

            // assert
            var actual = memStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriteData2()
        {
            // arrange
            var instance = new BodyChunkEncodingWriter();
            var memStream = new MemoryStream();
            Func<byte[], int, int, Task> sink = (data, offset, length) =>
            {
                memStream.Write(data, offset, length);
                return Task.CompletedTask;
            };
            var data = new byte[] { 3, 21, 16 };
            var expected = new byte[]
            {
                (byte)'0', (byte)'1', (byte)',',
                (byte)'0', (byte)'0', (byte)'0', (byte)'0',
                (byte)'0', (byte)'0', (byte)'0', (byte)'0',
                (byte)'0', (byte)'3',
                3, 21, 16
            };

            // act
            await instance.WriteData(data, sink);

            // assert
            var actual = memStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriteData3()
        {
            // arrange
            var instance = new BodyChunkEncodingWriter();
            var memStream = new MemoryStream();
            Func<byte[], int, int, Task> sink = (data, offset, length) =>
            {
                memStream.Write(data, offset, length);
                return Task.CompletedTask;
            };
            var data = new byte[] { 3, 21, 16, 27, 48, 50, 91 };
            var expected = new byte[]
            {
                (byte)'0', (byte)'1', (byte)',',
                (byte)'0', (byte)'0', (byte)'0', (byte)'0',
                (byte)'0', (byte)'0', (byte)'0', (byte)'0',
                (byte)'0', (byte)'4',
                16, 27, 48, 50
            };

            // act
            await instance.WriteData(data, 2, 4, sink);

            // assert
            var actual = memStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestInternals1()
        {
            // arrange
            var instance = new BodyChunkEncodingWriter();
            var offsets = new List<int>();
            var lengths = new List<int>();
            var dataList = new List<bool>();
            Func<byte[], int, int, Task> sink = (data, offset, length) =>
            {
                dataList.Add(data != null);
                offsets.Add(offset);
                lengths.Add(length);
                return Task.CompletedTask;
            };
            var expectedOffsets = new List<int>
            {
                0, 0, 0, 1_000_000_000
            };
            var expectedLengths = new List<int>
            {
                13, 1_000_000_000, 13, 100_000_000
            };
            var expectedDataList = new List<bool>
            {
                true, false, true, false
            };

            // act
            await instance.EncodeBodyChunkV1(null, 0, 1_100_000_000,
                sink);

            // assert
            Assert.Equal(expectedLengths, lengths);
            Assert.Equal(expectedOffsets, offsets);
            Assert.Equal(expectedDataList, dataList);
        }

        [Fact]
        public async Task TestInternals2()
        {
            // arrange
            var instance = new BodyChunkEncodingWriter();
            var offsets = new List<int>();
            var lengths = new List<int>();
            var dataList = new List<bool>();
            Func<byte[], int, int, Task> sink = (data, offset, length) =>
            {
                dataList.Add(data != null);
                offsets.Add(offset);
                lengths.Add(length);
                return Task.CompletedTask;
            };
            var expectedOffsets = new List<int>
            {
                0, 27, 0, 1_000_000_027, 0, 2_000_000_027
            };
            var expectedLengths = new List<int>
            {
                13, 1_000_000_000, 13, 1_000_000_000, 13, 130_000_004
            };
            var expectedDataList = new List<bool>
            {
                true, false, true, false, true, false
            };

            // act
            await instance.EncodeBodyChunkV1(null, 27, 2_130_000_004,
                sink);

            // assert
            Assert.Equal(expectedLengths, lengths);
            Assert.Equal(expectedOffsets, offsets);
            Assert.Equal(expectedDataList, dataList);
        }
    }
}
