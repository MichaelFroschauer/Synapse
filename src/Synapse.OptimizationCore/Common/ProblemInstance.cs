using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common;

public class ProblemInstance : IProblemInstance
{
    public required string Name { get; init; }
    public required ProblemType ProblemType { get; init; }
}
