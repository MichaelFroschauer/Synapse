namespace Synapse.OptimizationCore.HITL;

/// <summary>
/// A trigger that evaluates the current search state and decides whether the
/// algorithm should pause for human interaction.
/// </summary>
public interface IInteractionTrigger
{
    /// <summary>Human-readable name of this trigger (shown in the GUI).</summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the current search context and returns <c>true</c> when the
    /// algorithm should pause to ask for user input.
    /// </summary>
    bool ShouldTrigger(HitlTriggerContext context);

    /// <summary>
    /// Returns a human-readable reason when the trigger last fired.
    /// Null if it has not fired yet.
    /// </summary>
    string? LastTriggerReason { get; }
}

