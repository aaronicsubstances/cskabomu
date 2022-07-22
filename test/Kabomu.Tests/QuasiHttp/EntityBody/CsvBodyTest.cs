using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class CsvBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var content = new Dictionary<string, List<string>>();
            var instance = new CsvBody(content, "text/csv");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, -1, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var content = new Dictionary<string, List<string>>
            {
                { "A", new List<string> {"b", "2"} },
                { "B", new List<string> { "2"} },
                { "C", new List<string>() },
                { "D", new List<string>{ "Fire" } }
            };
            var instance = new CsvBody(content, "text/plain");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(16, instance, -1, "text/plain",
                new int[] { 16, 3 }, null, Encoding.UTF8.GetBytes("A,b,2\nB,2\nC\nD,Fire\n"));
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new StringBody(null, null);
            });
            var content = new Dictionary<string, List<string>>
            {
                { "c", new List<string> { "2"} },
            };
            var instance = new CsvBody(content, null);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
