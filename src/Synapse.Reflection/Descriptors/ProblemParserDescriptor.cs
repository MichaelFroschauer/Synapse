using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Reflection.Descriptors;

public class ProblemParserDescriptor : BaseDescriptor
{
    public ProblemParserDescriptor(Type type, ProblemParserAttribute attr) : base(type, attr)
    {
        ProblemType = attr.ProblemType;
    }

    public ProblemType ProblemType { get; init; }
}
