using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public class ConfigurablePathConstraint : IPathConstraint
    {
        public IContext ExpectedContext { get; set; }
        internal DefaultPathTemplateInternal ExpectedPathTemplate { get; set; }
        public IDictionary<string, string> ExpectedValues { get; set; }
        public string ExpectedValueKey { get; set; }
        public int ExpectedDirection { get; set; }
        public bool? ReturnValue { get; set; }
        public List<string> ConstraintArgLogs { get; set; }

        public bool Match(IContext context, IPathTemplate pathTemplate,
            IDictionary<string, string> values, string valueKey,
            string[] constraintArgs, int direction)
        {
            if (ExpectedContext != null)
            {
                Assert.Equal(ExpectedContext, context);
            }
            if (ExpectedPathTemplate != null)
            {
                Assert.Equal(ExpectedPathTemplate, pathTemplate);
            }
            if (ExpectedValues != null)
            {
                Assert.Equal(ExpectedValues, values);
            }
            if (ExpectedValueKey != null)
            {
                Assert.Equal(ExpectedValueKey, valueKey);
            }
            if (ExpectedDirection != 0)
            {
                Assert.Equal(ExpectedDirection, direction);
            }
            if (constraintArgs != null)
            {
                ConstraintArgLogs.Add(string.Join(",", constraintArgs));
            }
            if (ReturnValue.HasValue)
            {
                return ReturnValue.Value;
            }
            throw new NotImplementedException();
        }
    }
}
