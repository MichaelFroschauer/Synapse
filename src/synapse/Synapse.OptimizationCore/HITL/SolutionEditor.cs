using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.HITL;

public class SolutionEditor
{
    public string? Name { get; init; }
    public required ISolution SolutionPrototype { get; init; }
    public required double EditProbability { get; set; } = 1.0;
    public required IManualEditApplier EditApplier { get; init; }
    public required int ExecuteForNrOfIterations { get; init; } = 0;
    public required int ExecutedForNrOfIterations { get; set; } = 0;
}
