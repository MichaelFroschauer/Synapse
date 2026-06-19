using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AlgorithmAttribute : DisplayInfoAttribute
{
    public AlgorithmCategory Category { get; set; } = AlgorithmCategory.Default;
    public string IconPath { get; set; } = string.Empty;
    public AlgorithmType AlgorithmType { get; set; } = AlgorithmType.Unknown;
}

public enum AlgorithmCategory
{
    Default,
    PopulationBased
}
