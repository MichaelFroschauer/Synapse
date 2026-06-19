namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// Triggers interaction when no fitness improvement has been observed for a
/// configurable number of consecutive iterations (stagnation window).
/// </summary>
public class StagnationTrigger : IInteractionTrigger
{
    public string Name => "Stagnation Detection";
    public string? LastTriggerReason { get; private set; }

    /// <summary>
    /// Number of iterations without improvement before firing.
    /// </summary>
    public int StagnationWindow { get; set; } = 50;

    /// <summary>
    /// Minimum relative improvement (as fraction of current best) to count as "not stagnant".
    /// For example 0.001 means at least 0.1% improvement is needed.
    /// </summary>
    public double MinImprovementFraction { get; set; } = 0.0001;

    /// <summary>
    /// After firing, do not fire again for at least this many iterations (cooldown).
    /// Prevents repeated pauses in quick succession.
    /// </summary>
    public int CooldownIterations { get; set; } = 20;

    private int _lastFiredIteration = -1;

    public StagnationTrigger() { }
    public StagnationTrigger(int stagnationWindow, double minImprovementFraction = 0.0001, int cooldown = 20)
    {
        StagnationWindow = Math.Max(2, stagnationWindow);
        MinImprovementFraction = Math.Max(0, minImprovementFraction);
        CooldownIterations = Math.Max(0, cooldown);
    }

    public bool ShouldTrigger(HitlTriggerContext context)
    {
        if (context.FitnessHistory.Count < StagnationWindow) return false;

        // Cooldown check
        if (_lastFiredIteration >= 0 && (context.Iteration - _lastFiredIteration) < CooldownIterations)
            return false;

        // Look at the last StagnationWindow entries
        var history = context.FitnessHistory;
        int start = history.Count - StagnationWindow;
        double oldest = history[start];
        double newest = history[^1];

        // Check if there is any meaningful improvement across the window
        double range = Math.Abs(oldest);
        if (range < 1e-12) range = 1.0; // avoid division by zero

        double improvement = Math.Abs(newest - oldest) / range;

        if (improvement < MinImprovementFraction)
        {
            _lastFiredIteration = context.Iteration;
            LastTriggerReason = $"Stagnation detected: no significant improvement over last {StagnationWindow} iterations " +
                                $"(improvement: {improvement:P2}, threshold: {MinImprovementFraction:P2})";
            return true;
        }

        return false;
    }
}

