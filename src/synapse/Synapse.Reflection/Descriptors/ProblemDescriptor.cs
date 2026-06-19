using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;

namespace Synapse.Reflection.Descriptors;

public class ProblemDescriptor : BaseDescriptor
{
    public List<ProblemInstanceDescriptor> ProblemInstances { get; init; } = new();
    public ProblemType ProblemType { get; init; }

    public ProblemDescriptor(Type type, ProblemAttribute attr) : base(type, attr)
    {
        ImplementationType = type;
        Name = attr?.Name ?? type.Name;
        Description = attr?.Description ?? "";
        ProblemType = attr?.ProblemType ?? ProblemType.Unknown;
    }
}
