namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// Triggers interaction when population diversity drops below a configurable threshold.
/// Useful to detect premature convergence and allow the user to inject diversity.
/// </summary>
public class DiversityLossTrigger : IInteractionTrigger
{
    public string Name => "Diversity Loss";
    public string? LastTriggerReason { get; private set; }

    /// <summary>
    /// Diversity threshold in [0,1]. When diversity falls below this value, the trigger fires.
    /// </summary>
    public double DiversityThreshold { get; set; } = 0.2;

    /// <summary>
    /// Number of consecutive iterations the diversity must stay below the threshold
    /// before the trigger fires. Prevents firing on transient dips.
    /// </summary>
    public int ConsecutiveIterations { get; set; } = 5;

    /// <summary>
    /// After firing, do not fire again for at least this many iterations.
    /// </summary>
    public int CooldownIterations { get; set; } = 30;

    private int _lastFiredIteration = -1;

    public DiversityLossTrigger() { }
    public DiversityLossTrigger(double diversityThreshold, int consecutiveIterations = 5, int cooldown = 30)
    {
        DiversityThreshold = Math.Clamp(diversityThreshold, 0.0, 1.0);
        ConsecutiveIterations = Math.Max(1, consecutiveIterations);
        CooldownIterations = Math.Max(0, cooldown);
    }

    public bool ShouldTrigger(HitlTriggerContext context)
    {
        if (context.DiversityHistory.Count < ConsecutiveIterations) return false;

        // Cooldown check
        if (_lastFiredIteration >= 0 && (context.Iteration - _lastFiredIteration) < CooldownIterations)
            return false;

        // Check if the last N diversity values are all below the threshold
        var history = context.DiversityHistory;
        int start = history.Count - ConsecutiveIterations;
        for (int i = start; i < history.Count; i++)
        {
            if (history[i] >= DiversityThreshold)
                return false;
        }

        double currentDiversity = history[^1];
        _lastFiredIteration = context.Iteration;
        LastTriggerReason = $"Diversity loss detected: diversity {currentDiversity:F3} is below threshold {DiversityThreshold:F3} " +
                            $"for {ConsecutiveIterations} consecutive iterations";
        return true;
    }
}

