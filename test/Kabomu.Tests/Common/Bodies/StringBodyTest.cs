using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class StringBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var instance = new StringBody("", "text/csv");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, -1, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var instance = new StringBody("Ab2", null);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "text/plain",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StringBody(null, null);
            });
            var instance = new StringBody("c2", null);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
