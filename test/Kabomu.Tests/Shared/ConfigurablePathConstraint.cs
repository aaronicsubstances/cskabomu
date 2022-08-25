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
        public bool ReturnValue { get; set; }
        public List<string> ConstraintArgLogs { get; set; }

        public bool Match(IContext context, IPathTemplate pathTemplate,
            IDictionary<string, string> values, string valueKey,
            string[] constraintArgs, int direction)
        {
            Assert.Equal(ExpectedContext, context);
            Assert.Equal(ExpectedPathTemplate, pathTemplate);
            Assert.Equal(ExpectedValues, values);
            Assert.Equal(ExpectedValueKey, valueKey);
            Assert.Equal(ExpectedDirection, direction);

            ConstraintArgLogs?.Add(string.Join(",", constraintArgs));

            return ReturnValue;
        }
    }
}
