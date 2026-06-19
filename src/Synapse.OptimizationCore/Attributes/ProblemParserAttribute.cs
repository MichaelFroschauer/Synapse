using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ProblemParserAttribute : DisplayInfoAttribute
{
    public required ProblemType ProblemType { get; set; }
}
