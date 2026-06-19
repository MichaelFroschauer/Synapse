using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.Interfaces;

public interface IProblemInstance
{
    string Name { get; init; }
    ProblemType ProblemType { get; init; }
}
