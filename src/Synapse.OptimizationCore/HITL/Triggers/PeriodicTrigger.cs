namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// Triggers interaction every <see cref="Interval"/> iterations.
/// This is the simplest time-triggered strategy.
/// </summary>
public class PeriodicTrigger : IInteractionTrigger
{
    public string Name => "Periodic";
    public string? LastTriggerReason { get; private set; }

    /// <summary>
    /// Pause every N iterations. Must be >= 1.
    /// </summary>
    public int Interval { get; set; } = 50;

    private int _lastFiredIteration = -1;

    public PeriodicTrigger() { }
    public PeriodicTrigger(int interval) => Interval = Math.Max(1, interval);

    public bool ShouldTrigger(HitlTriggerContext context)
    {
        if (Interval <= 0) return false;
        if (context.Iteration > 0 && context.Iteration % Interval == 0 && context.Iteration != _lastFiredIteration)
        {
            _lastFiredIteration = context.Iteration;
            LastTriggerReason = $"Periodic checkpoint at iteration {context.Iteration} (every {Interval} iterations)";
            return true;
        }
        return false;
    }
}


