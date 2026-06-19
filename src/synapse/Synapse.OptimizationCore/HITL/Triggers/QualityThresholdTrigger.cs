namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// Triggers interaction when the best fitness reaches or surpasses a target value.
/// Useful for stopping and refining once a "good enough" solution is found.
/// </summary>
public class QualityThresholdTrigger : IInteractionTrigger
{
    public string Name => "Quality Threshold";
    public string? LastTriggerReason { get; private set; }

    /// <summary>
    /// Target fitness value. When the best fitness reaches this value (or better), the trigger fires.
    /// </summary>
    public double TargetFitness { get; set; } = 100.0;

    /// <summary>
    /// If true, "better" means lower fitness (minimization). If false, "better" means higher (maximization).
    /// </summary>
    public bool Minimize { get; set; } = true;

    /// <summary>
    /// Fire only once. After the first trigger, the trigger will not fire again.
    /// </summary>
    public bool FireOnce { get; set; } = true;

    private bool _hasFired;

    public QualityThresholdTrigger() { }
    public QualityThresholdTrigger(double targetFitness, bool minimize = true, bool fireOnce = true)
    {
        TargetFitness = targetFitness;
        Minimize = minimize;
        FireOnce = fireOnce;
    }

    public bool ShouldTrigger(HitlTriggerContext context)
    {
        if (FireOnce && _hasFired) return false;

        bool reached = Minimize
            ? context.BestFitness <= TargetFitness
            : context.BestFitness >= TargetFitness;

        if (reached)
        {
            _hasFired = true;
            LastTriggerReason = $"Quality threshold reached: best fitness {context.BestFitness:F4} " +
                                $"{(Minimize ? "<=" : ">=")} target {TargetFitness:F4}";
            return true;
        }

        return false;
    }
}

