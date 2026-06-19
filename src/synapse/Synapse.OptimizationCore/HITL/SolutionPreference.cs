using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.HITL;

public class SolutionPreference
{
    public required string Name { get; init; }
    public required double Weight { get; set; }
    public Func<ISolution, bool>? MatcherFunction { get; init; }

    public bool HasSolutionSimilarity { get; init; } = false;
    public ISolutionSimilarity? SimilarityEvaluator { get; init; }
    public ISolution? ReferenceSolution { get; init; }
}
