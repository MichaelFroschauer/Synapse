using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;

namespace Synapse.HITL.Problems.QAP;

[DisplayInfo("QAP Fixed Facility Assignment Constraint",
    "Pins a specific facility to an exact location. The solver must always place this facility at the given location. "
    + "During repair, the facility is swapped with whatever currently occupies the desired location.")]
[ApplicableSolution(typeof(QapSolution))]
public class QapFixAssignmentConstraint : IConstraint
{
    private readonly int _facility;
    private readonly int _location;

    public QapFixAssignmentConstraint([DisplayInfo("Facility")] int facility, [DisplayInfo("Location")] int location)
    {
        _facility = facility;
        _location = location;
    }
    
    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapFixAssignmentConstraint expects a QapSolution", nameof(solution));

        if (_facility < 0 || _facility >= perm.Length)
            throw new ArgumentOutOfRangeException(nameof(_facility));

        int loc = perm.GetParameterWithType(_facility);
        return loc == _location;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapFixAssignmentConstraint expects a QapSolution", nameof(solution));

        int n = perm.Length;
        if (_facility < 0 || _facility >= n)
            throw new ArgumentOutOfRangeException(nameof(_facility));
        if (_location < 0 || _location >= n) return null;

        int currentLoc = perm.GetParameterWithType(_facility);
        if (currentLoc == _location) return solution; // already satisfied

        var inverse = ConstraintHelpers.BuildInverse(perm);
        int occupant = inverse[_location]; // facility currently at desired location
        if (occupant == -1) return null; // defensive

        // perform a swap: move _facility -> _location, occupant -> currentLoc
        var repaired = (QapSolution)solution.Clone();
        repaired.SetParameter(_facility, new Parameter(_location));
        repaired.SetParameter(occupant, new Parameter(currentLoc));
        return IsSatisfied(repaired) ? repaired : null;
    }

    public string Description => $"QAP fixed assignment: facility {_facility} -> location {_location}";

    public string TextVisualization
    {
        get
        {
            return
                $@"QAP Fixed Assignment Constraint
Facility: {_facility}
Required location: {_location}

Description: This constraint pins one facility to one exact location.
Repair behavior: If enabled, the facility is swapped with the current occupant of the required location.";
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
}
