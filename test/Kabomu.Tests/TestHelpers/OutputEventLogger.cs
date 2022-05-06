using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class OutputEventLogger
    {
        public List<string> Logs { get; set; }

        public void AssertEqual(List<string> expectedLogs, ITestOutputHelper outputHelper)
        {
            int minCount = Math.Min(Logs.Count, expectedLogs.Count);
            try
            {
                Assert.Equal(expectedLogs.GetRange(0, minCount), Logs.GetRange(0, minCount));
                Assert.Equal(expectedLogs.Count, Logs.Count);
            }
            catch (Exception)
            {
                if (outputHelper != null)
                {
                    outputHelper.WriteLine("Expected:");
                    outputHelper.WriteLine(string.Join(Environment.NewLine, expectedLogs));
                    outputHelper.WriteLine("Actual:");
                    outputHelper.WriteLine(string.Join(Environment.NewLine, Logs));
                }
                throw;
            }
        }
    }
}
