using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ProblemInstanceAttribute : DisplayInfoAttribute
{
    public required ProblemType ProblemType { get; set; }
}
