using Kabomu.Exceptions;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests
{
    public class IOUtilsInternalTest
    {
        [Fact]
        public async Task TestReadBytesFully()
        {
            // arrange
            var reader = new RandomizedReadInputStream(
                new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
                false);
            var readBuffer = new byte[6];

            // act
            await IOUtilsInternal.ReadBytesFully(reader, readBuffer, 0, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 0, 1, 2 }, 0, 3,
                readBuffer, 0, 3);

            // assert that zero length reading doesn't cause problems.
            await IOUtilsInternal.ReadBytesFully(reader, readBuffer, 3, 0);

            // act again
            await IOUtilsInternal.ReadBytesFully(reader, readBuffer, 1, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 3, 4, 5 }, 0, 3,
                readBuffer, 1, 3);

            // act again
            await IOUtilsInternal.ReadBytesFully(reader, readBuffer, 3, 2);

            // assert
            ComparisonUtils.CompareData(new byte[] { 6, 7 }, 0, 2,
                readBuffer, 3, 2);

            // test zero byte reads.
            readBuffer = new byte[] { 2, 3, 5, 8 };
            await IOUtilsInternal.ReadBytesFully(reader, readBuffer, 0, 0);
            Assert.Equal(new byte[] { 2, 3, 5, 8 }, readBuffer);
        }

        [Fact]
        public async Task TestReadBytesFullyForErrors()
        {
            // arrange
            var reader = new RandomizedReadInputStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var readBuffer = new byte[5];

            // act
            await IOUtilsInternal.ReadBytesFully(reader, readBuffer, 0, readBuffer.Length);

            // assert
            ComparisonUtils.CompareData(
                new byte[] { 0, 1, 2, 3, 4 }, 0, readBuffer.Length,
                readBuffer, 0, readBuffer.Length);

            // act and assert unexpected end of read
            var actualEx = await Assert.ThrowsAsync<KabomuIOException>(() =>
                IOUtilsInternal.ReadBytesFully(reader, readBuffer, 0, readBuffer.Length));
            Assert.Contains("end of read", actualEx.Message);
        }

        [Fact]
        public void TestReadBytesFullySync()
        {
            // arrange
            var reader = new RandomizedReadInputStream(
                new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
                false);
            var readBuffer = new byte[6];

            // act
            IOUtilsInternal.ReadBytesFullySync(reader, readBuffer, 0, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 0, 1, 2 }, 0, 3,
                readBuffer, 0, 3);

            // assert that zero length reading doesn't cause problems.
            IOUtilsInternal.ReadBytesFullySync(reader, readBuffer, 3, 0);

            // act again
            IOUtilsInternal.ReadBytesFullySync(reader, readBuffer, 1, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 3, 4, 5 }, 0, 3,
                readBuffer, 1, 3);

            // act again
            IOUtilsInternal.ReadBytesFullySync(reader, readBuffer, 3, 2);

            // assert
            ComparisonUtils.CompareData(new byte[] { 6, 7 }, 0, 2,
                readBuffer, 3, 2);

            // test zero byte reads.
            readBuffer = new byte[] { 2, 3, 5, 8 };
            IOUtilsInternal.ReadBytesFullySync(reader, readBuffer, 0, 0);
            Assert.Equal(new byte[] { 2, 3, 5, 8 }, readBuffer);
        }

        [Fact]
        public void TestReadBytesFullySyncForErrors()
        {
            // arrange
            var reader = new RandomizedReadInputStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var readBuffer = new byte[5];

            // act
            IOUtilsInternal.ReadBytesFullySync(reader, readBuffer, 0, readBuffer.Length);

            // assert
            ComparisonUtils.CompareData(
                new byte[] { 0, 1, 2, 3, 4 }, 0, readBuffer.Length,
                readBuffer, 0, readBuffer.Length);

            // act and assert unexpected end of read
            var actualEx = Assert.Throws<KabomuIOException>(() =>
                IOUtilsInternal.ReadBytesFullySync(reader, readBuffer, 0, readBuffer.Length));
            Assert.Contains("end of read", actualEx.Message);
        }
    }
}
