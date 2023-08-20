using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.IntegrationTests.Common
{
    public class ComparisonUtilsTest
    {
        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed1()
        {
            var tasks = new List<Task>();
            await ComparisonUtils.WhenAnyFailOrAllSucceed(tasks);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed3()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.FromException(new Exception("error3"))
            };
            var actualEx = await Assert.ThrowsAnyAsync<Exception>(() =>
                ComparisonUtils.WhenAnyFailOrAllSucceed(tasks));
            Assert.Equal("error3", actualEx.Message);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed4()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.FromException(new Exception("error4a")),
                Task.FromException(new Exception("error4b")),
                Task.CompletedTask
            };
            var actualEx = await Assert.ThrowsAnyAsync<Exception>(() =>
                ComparisonUtils.WhenAnyFailOrAllSucceed(tasks));
            Assert.Equal("error4a", actualEx.Message);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed5()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.FromException(new Exception("error5")),
                Task.CompletedTask
            };
            var actualEx = await Assert.ThrowsAnyAsync<Exception>(() =>
                ComparisonUtils.WhenAnyFailOrAllSucceed(tasks));
            Assert.Equal("error5", actualEx.Message);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed6()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.Delay(1000),
                Task.CompletedTask
            };
            await ComparisonUtils.WhenAnyFailOrAllSucceed(tasks);
        }
    }
}
