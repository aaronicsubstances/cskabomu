using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class HtmlFormUrlEncodedBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var content = new Dictionary<string, List<string>>();
            var instance = new HtmlFormUrlEncodedBody(content);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(0, instance, -1, "application/x-www-form-urlencoded",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var content = new Dictionary<string, List<string>>
            {
                { "A", new List<string> {"b", "2"} },
                { "B", new List<string> { "2"} },
                { "C", new List<string>() },
                { "D", new List<string>{ "Fire" } }
            };
            var instance = new HtmlFormUrlEncodedBody(content);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(16, instance, -1, "application/x-www-form-urlencoded",
                new int[] { 16, 3 }, null, Encoding.UTF8.GetBytes("A,b,2\nB,2\nC\nD,Fire\n"));
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StringBody(null, null);
            });
            var content = new Dictionary<string, List<string>>
            {
                { "c", new List<string> { "2"} },
            };
            var instance = new HtmlFormUrlEncodedBody(content);
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
