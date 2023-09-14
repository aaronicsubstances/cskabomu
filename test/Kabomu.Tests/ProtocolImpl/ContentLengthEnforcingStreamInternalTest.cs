using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class ContentLengthEnforcingStreamInternalTest
    {
        [InlineData(0, "", "")]
        [InlineData(0, "a", "")]
        [InlineData(1, "ab", "a")]
        [InlineData(2, "ab", "ab")]
        [InlineData(2, "abc", "ab")]
        [InlineData(3, "abc", "abc")]
        [InlineData(4, "abcd", "abcd")]
        [InlineData(5, "abcde", "abcde")]
        [InlineData(6, "abcdefghi", "abcdef")]
        [Theory]
        public async Task TestReading(long contentLength, string srcData,
            string expected)
        {
            // 1. arrange
            var stream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(srcData));
            var instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act
            var actual = await ComparisonUtils.ReadToString(instance, false);

            // assert
            Assert.Equal(expected, actual);

            // 2. arrange again with old style async
            stream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(srcData));
            instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act
            actual = await ComparisonUtils.ReadToString(instance, true);

            // assert
            Assert.Equal(expected, actual);

            // 3. arrange again with sync
            stream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(srcData));
            instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act
            actual = ComparisonUtils.ReadToStringSync(instance, false);

            // assert
            Assert.Equal(expected, actual);

            // 3. arrange again with slow sync
            stream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(srcData));
            instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act
            actual = ComparisonUtils.ReadToStringSync(instance, true);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestForCancellation()
        {
            // 1. arrange
            var stream = new MemoryStream(MiscUtilsInternal.StringToBytes(
                "sizable"));
            var instance = new ContentLengthEnforcingStreamInternal(stream,
                5);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await instance.ReadAsync(new byte[2], cts.Token));
        }

        [InlineData(2, "")]
        [InlineData(4, "abc")]
        [InlineData(5, "abcd")]
        [InlineData(15, "abcdef")]
        [Theory]
        public async Task TestReadingForErrors(long contentLength, string srcData)
        {
            // 1. arrange
            var stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            var instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act and assert
            var actualEx = await Assert.ThrowsAsync<KabomuIOException>(
                () => ComparisonUtils.ReadToBytes(instance, false));
            Assert.Contains($"length of {contentLength}", actualEx.Message);

            // 2. arrange with old style async
            stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act and assert
            actualEx = await Assert.ThrowsAsync<KabomuIOException>(
                () => ComparisonUtils.ReadToBytes(instance, true));
            Assert.Contains($"length of {contentLength}", actualEx.Message);

            // 3. arrange with sync
            stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act and assert
            actualEx = Assert.Throws<KabomuIOException>(
                () => ComparisonUtils.ReadToBytesSync(instance, false));
            Assert.Contains($"length of {contentLength}", actualEx.Message);

            // 4. arrange with slow sync
            stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            instance = new ContentLengthEnforcingStreamInternal(stream,
                contentLength);

            // act and assert
            actualEx = Assert.Throws<KabomuIOException>(
                () => ComparisonUtils.ReadToBytesSync(instance, true));
            Assert.Contains($"length of {contentLength}", actualEx.Message);
        }

        [Fact]
        public async Task TestZeroByteRead1()
        {
            // 1. test with async.
            var stream = new MemoryStream(new byte[] { 0, 1, 2 });
            var instance = new ContentLengthEnforcingStreamInternal(stream, 3);

            var actualCount = await instance.ReadAsync(new byte[0], 0, 0);
            Assert.Equal(0, actualCount);

            var actual = new byte[3];
            await IOUtilsInternal.ReadBytesFully(instance,
                actual, 0, 3);
            Assert.Equal(new byte[] { 0, 1, 2 }, actual);

            actualCount = await instance.ReadAsync(new byte[0], 0, 0);
            Assert.Equal(0, actualCount);

            // 2. test with sync.
            stream = new MemoryStream(new byte[] { 0, 1, 2 });
            instance = new ContentLengthEnforcingStreamInternal(stream, 3);

            actualCount = instance.Read(new byte[0], 0, 0);
            Assert.Equal(0, actualCount);

            actual = new byte[3];
            IOUtilsInternal.ReadBytesFullySync(instance,
                actual, 0, 3);
            Assert.Equal(new byte[] { 0, 1, 2 }, actual);

            actualCount = instance.Read(new byte[0], 0, 0);
            Assert.Equal(0, actualCount);
        }

        [Fact]
        public async Task TestZeroByteRead2()
        {
            // 1. test with async.
            var stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5 });
            var reader = new RandomizedReadInputStream(stream);
            var instance = new ContentLengthEnforcingStreamInternal(reader, 4);

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                instance.ReadAsync(new byte[0], 0, 0));

            var expected = new byte[4];
            await IOUtilsInternal.ReadBytesFully(instance,
                expected, 0, expected.Length);
            Assert.Equal(expected, new byte[] { 0, 1, 2, 3 });

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                instance.ReadAsync(new byte[0], 0, 0));

            var actualCount = await instance.ReadAsync(new byte[2], 0, 2);
            Assert.Equal(0, actualCount);

            // 2. test with sync.
            stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 5 });
            reader = new RandomizedReadInputStream(stream);
            instance = new ContentLengthEnforcingStreamInternal(reader, 4);

            Assert.Throws<NotSupportedException>(() =>
                instance.Read(new byte[0], 0, 0));

            expected = new byte[4];
            IOUtilsInternal.ReadBytesFullySync(instance,
                expected, 0, expected.Length);
            Assert.Equal(expected, new byte[] { 0, 1, 2, 3 });

            Assert.Throws<NotSupportedException>(() =>
                instance.Read(new byte[0], 0, 0));

            actualCount = instance.Read(new byte[2], 0, 2);
            Assert.Equal(0, actualCount);
        }
    }
}
