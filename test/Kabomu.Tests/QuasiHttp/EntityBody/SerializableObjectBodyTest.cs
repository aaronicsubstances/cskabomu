using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class SerializableObjectBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            Func<object, byte[]> serializationHandler = obj => Encoding.UTF8.GetBytes((string)obj);
            var instance = new SerializableObjectBody("", serializationHandler)
            {
                ContentType = "text/csv"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, -1, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            Func<object, byte[]> serializationHandler = obj => Encoding.UTF8.GetBytes((string)obj);
            var instance = new SerializableObjectBody("Ab2", serializationHandler)
            {
                ContentType = "application/json"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "application/json",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public async Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new SerializableObjectBody(null, _ => new byte[0]);
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                new SerializableObjectBody("", null);
            });
            Func<object, byte[]> serializationHandler = obj => Encoding.UTF8.GetBytes((string)obj);
            var instance = new SerializableObjectBody("c2", serializationHandler);
            await CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);

            // test for specific errors
            serializationHandler = obj => null;
            instance = new SerializableObjectBody("d", serializationHandler);
            await Assert.ThrowsAnyAsync<Exception>(() => instance.ReadBytes(new byte[1], 0, 1));

            serializationHandler = obj => throw new Exception("se err");
            instance = new SerializableObjectBody("d", serializationHandler);
            var actualError = await Assert.ThrowsAnyAsync<Exception>(() =>
                instance.ReadBytes(new byte[1], 0, 1));
            Assert.Equal("se err", actualError.Message);
        }
    }
}