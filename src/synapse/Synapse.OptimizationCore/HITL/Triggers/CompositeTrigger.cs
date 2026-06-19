namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// Combines multiple triggers using OR logic: if any child trigger fires,
/// the composite trigger fires. This allows combining strategies such as
/// "pause every 100 iterations OR on stagnation OR on diversity loss".
/// </summary>
public class CompositeTrigger : IInteractionTrigger
{
    public string Name => "Composite";
    public string? LastTriggerReason { get; private set; }

    private readonly List<IInteractionTrigger> _triggers = [];

    public IReadOnlyList<IInteractionTrigger> Triggers => _triggers;

    public CompositeTrigger() { }

    public CompositeTrigger(params IInteractionTrigger[] triggers)
    {
        _triggers.AddRange(triggers);
    }

    public CompositeTrigger(IEnumerable<IInteractionTrigger> triggers)
    {
        _triggers.AddRange(triggers);
    }

    public void Add(IInteractionTrigger trigger) => _triggers.Add(trigger);
    public void Remove(IInteractionTrigger trigger) => _triggers.Remove(trigger);
    public void Clear() => _triggers.Clear();

    public bool ShouldTrigger(HitlTriggerContext context)
    {
        foreach (var trigger in _triggers)
        {
            if (trigger.ShouldTrigger(context))
            {
                LastTriggerReason = trigger.LastTriggerReason;
                return true;
            }
        }
        return false;
    }
}

