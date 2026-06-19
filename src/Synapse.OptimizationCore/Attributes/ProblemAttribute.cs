using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ProblemAttribute : DisplayInfoAttribute
{
    public ProblemType ProblemType { get; set; } = ProblemType.Unknown;
}