using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared.Mediator
{
    public class ConfigurablePathConstraint : IPathConstraint
    {
        public IContext ExpectedContext { get; set; }
        internal DefaultPathTemplateInternal ExpectedPathTemplate { get; set; }
        public IDictionary<string, object> ExpectedValues { get; set; }
        public bool? ReturnValue { get; set; }
        public List<string> ConstraintLogs { get; set; }

        public bool ApplyCheck(IContext context, IPathTemplate pathTemplate,
            IDictionary<string, object> values, string valueKey,
            string[] constraintArgs)
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
            if (constraintArgs != null)
            {
                var log = valueKey;
                if (constraintArgs.Length > 0)
                {
                    log += "," + string.Join(",", constraintArgs);
                }
                ConstraintLogs.Add(log);
            }
            if (ReturnValue.HasValue)
            {
                return ReturnValue.Value;
            }
            throw new NotImplementedException();
        }
    }
}
