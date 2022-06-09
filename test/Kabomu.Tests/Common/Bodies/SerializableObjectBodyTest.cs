using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class SerializableObjectBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            Func<object, byte[]> serializationHandler = obj => Encoding.UTF8.GetBytes((string)obj);
            var instance = new SerializableObjectBody("", serializationHandler, "text/csv");

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(0, instance, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            Func<object, byte[]> serializationHandler = obj => Encoding.UTF8.GetBytes((string)obj);
            var instance = new SerializableObjectBody("Ab2", serializationHandler, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(2, instance, "application/json",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new SerializableObjectBody(null, _ => new byte[0], null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                new SerializableObjectBody("", null, null);
            });
            Func<object, byte[]> serializationHandler = obj => Encoding.UTF8.GetBytes((string)obj);
            var instance = new SerializableObjectBody("c2", serializationHandler, null);
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
            serializationHandler = obj => null;
            instance = new SerializableObjectBody("d", serializationHandler, null);
            var cbCalled = false;
            instance.ReadBytes(new TestEventLoopApi(), new byte[1], 0, 1, (e, len) =>
            {
                Assert.False(cbCalled);
                Assert.NotNull(e);
                cbCalled = true;
            });
            Assert.True(cbCalled);
            serializationHandler = obj => throw new Exception("se err");
            instance = new SerializableObjectBody("d", serializationHandler, null);
            cbCalled = false;
            instance.ReadBytes(new TestEventLoopApi(), new byte[1], 0, 1, (e, len) =>
            {
                Assert.False(cbCalled);
                Assert.NotNull(e);
                Assert.Equal("se err", e.Message);
                cbCalled = true;
            });
            Assert.True(cbCalled);
        }
    }
}