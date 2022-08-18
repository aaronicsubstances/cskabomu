using System.Collections.Generic;

namespace Kabomu.Mediator.Path
{
    public class DefaultPathTemplateSpecification
    {
        public IDictionary<string, string> DefaultValues { get; set; }
        public IList<DefaultPathTemplateExample> SampleSets { get; set; }
        public IDictionary<string, string> ConstraintSpecs { get; set; }
    }
}