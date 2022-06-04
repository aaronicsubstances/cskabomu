using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class StringBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var instance = new StringBody("", "text/csv");

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "text/csv",
                new int[0], null, "");
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var instance = new StringBody("Ab2", null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "text/plain",
                new int[] { 2, 1 }, null, "Ab2");
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StringBody(null, null);
            });
            var instance = new StringBody("c2", null);
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
