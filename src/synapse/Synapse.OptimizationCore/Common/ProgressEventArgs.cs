using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common;

/// <summary>
/// Event data published on each progress update from a running algorithm.
/// </summary>
public class ProgressEventArgs
{
    public IProblem? Problem { get; init; }
    public IAlgorithmConfig? Config { get; init; }
    public IAlgorithmController? AlgorithmController { get; init; }
    public IHitlController? HitlController { get; init; }
    public ISolution? BestSolution { get; init; }
    public double BestFitness { get; init; }
    public double BestRawFitness { get; init; }
    public ISolution? CurrentBestSolution { get; init; }
    public double CurrentBestFitness { get; init; }
    public double CurrentBestRawFitness { get; init; }
    public int Iteration { get; init; }
    public string? Message { get; init; }
    
    /// <summary>
    /// Population diversity in [0,1] for this iteration (0 = all identical, 1 = maximally diverse).
    /// Null when diversity was not computed (e.g. population not available).
    /// </summary>
    public double? Diversity { get; init; }
}
