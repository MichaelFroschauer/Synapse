using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.HITL;

// Hard constraint interface: algorithms should respect constraints; a repair is optional
public interface IConstraint
{
    /// <summary>
    /// Returns true if the solution satisfies the constraint
    /// </summary>
    bool IsSatisfied(ISolution solution);

    /// <summary>
    /// Attempt to repair the solution so it satisfies the constraint. Return repaired solution (or original if no repair performed).
    /// Implementations may return null to indicate the solution must be rejected.
    /// </summary>
    ISolution? Repair(ISolution solution);

    string Description { get; }
    
    string TextVisualization { get; }
    bool RepairSolution { get; set; }
}
