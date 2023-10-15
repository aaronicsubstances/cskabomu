using Kabomu.Exceptions;
using Kabomu.Tlv;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Tlv
{
    public class MaxLengthEnforcingStreamInternalTest
    {
        [InlineData(0, "")]
        [InlineData(0, "a")]
        [InlineData(2, "a")]
        [InlineData(2, "ab")]
        [InlineData(3, "a")]
        [InlineData(3, "abc")]
        [InlineData(4, "abcd")]
        [InlineData(5, "abcde")]
        [InlineData(60, "abcdefghi")]
        [Theory]
        public async Task TestReading(int maxLength, string expected)
        {
            // 1. arrange
            var stream = new RandomizedReadInputStream(expected);
            var instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

            // act
            var actual = await ComparisonUtils.ReadToString(instance, false);

            // assert
            Assert.Equal(expected, actual);

            // 2. arrange again with old style async
            stream = new RandomizedReadInputStream(expected);
            instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

            // act
            actual = await ComparisonUtils.ReadToString(instance, true);

            // assert
            Assert.Equal(expected, actual);

            // 3. arrange again with sync
            stream = new RandomizedReadInputStream(expected);
            instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

            // act
            actual = ComparisonUtils.ReadToStringSync(instance, false);

            // assert
            Assert.Equal(expected, actual);

            // 4. arrange again with slow sync
            stream = new RandomizedReadInputStream(expected);
            instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

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
            var instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                5);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await instance.ReadAsync(new byte[2], cts.Token));
        }

        [InlineData(1, "ab")]
        [InlineData(2, "abc")]
        [InlineData(3, "abcd")]
        [InlineData(5, "abcdefxyz")]
        [Theory]
        public async Task TestReadingForErrors(int maxLength, string srcData)
        {
            // 1. arrange
            var stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            var instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

            // act and assert
            var actualEx = await Assert.ThrowsAsync<KabomuIOException>(
                () => ComparisonUtils.ReadToBytes(instance, false));
            Assert.Contains($"exceeds limit of {maxLength}", actualEx.Message);

            // 2. arrange with old style async
            stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

            // act and assert
            actualEx = await Assert.ThrowsAsync<KabomuIOException>(
                () => ComparisonUtils.ReadToBytes(instance, true));
            Assert.Contains($"exceeds limit of {maxLength}", actualEx.Message);

            // 3. arrange with sync
            stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

            // act and assert
            actualEx = Assert.Throws<KabomuIOException>(
                () => ComparisonUtils.ReadToBytesSync(instance, false));
            Assert.Contains($"exceeds limit of {maxLength}", actualEx.Message);

            // 4. arrange with slow sync
            stream = new MemoryStream(MiscUtilsInternal.StringToBytes(srcData));
            instance = TlvUtils.CreateMaxLengthEnforcingStream(stream,
                maxLength);

            // act and assert
            actualEx = Assert.Throws<KabomuIOException>(
                () => ComparisonUtils.ReadToBytesSync(instance, true));
            Assert.Contains($"exceeds limit of {maxLength}", actualEx.Message);
        }

        [Fact]
        public async Task TestZeroByteRead1()
        {
            // 1. test with async.
            var stream = new MemoryStream(new byte[] { 0, 1, 2 });
            var instance = TlvUtils.CreateMaxLengthEnforcingStream(stream, 3);

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
            instance = TlvUtils.CreateMaxLengthEnforcingStream(stream, 3);

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
            var stream = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            var reader = new RandomizedReadInputStream(stream);
            var instance = TlvUtils.CreateMaxLengthEnforcingStream(reader, 10);

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
            stream = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            reader = new RandomizedReadInputStream(stream);
            instance = TlvUtils.CreateMaxLengthEnforcingStream(reader, 10);

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
