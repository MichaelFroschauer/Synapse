namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// Carries the current search state to interaction triggers so they can decide
/// whether to request a pause for human input.
/// </summary>
public class HitlTriggerContext
{
    /// <summary>Current iteration / generation number.</summary>
    public int Iteration { get; init; }

    /// <summary>Best fitness value found so far (global best).</summary>
    public double BestFitness { get; init; }

    /// <summary>Best fitness of the current iteration / generation.</summary>
    public double CurrentBestFitness { get; init; }

    /// <summary>
    /// Population diversity in [0,1] for the current iteration.
    /// Null when diversity was not computed.
    /// </summary>
    public double? Diversity { get; init; }

    /// <summary>
    /// Rolling history of best fitness values per iteration (most recent last).
    /// Used by stagnation triggers to detect lack of improvement.
    /// </summary>
    public IReadOnlyList<double> FitnessHistory { get; init; } = [];

    /// <summary>
    /// Rolling history of diversity values per iteration (most recent last).
    /// Used by diversity-loss triggers to detect convergence.
    /// </summary>
    public IReadOnlyList<double> DiversityHistory { get; init; } = [];
}

