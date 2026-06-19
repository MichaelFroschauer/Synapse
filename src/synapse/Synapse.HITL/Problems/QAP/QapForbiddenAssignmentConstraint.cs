using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;

namespace Synapse.HITL.Problems.QAP;

[DisplayInfo("QAP Forbidden Facility Assignment Constraint",
    "Prevents a specific facility from being placed at a certain location. "
    + "During repair, the facility is swapped with another facility so it no longer occupies the forbidden location.")]
[ApplicableSolution(typeof(QapSolution))]
public class QapForbiddenAssignmentConstraint : IConstraint
{
    private readonly int _facility;
    private readonly int _location;

    public QapForbiddenAssignmentConstraint([DisplayInfo("Facility")] int facility, [DisplayInfo("Location")] int location)
    {
        _facility = facility;
        _location = location;
    }
    
    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapForbiddenAssignmentConstraint expects a QapSolution", nameof(solution));

        if (_facility < 0 || _facility >= perm.Length)
            throw new ArgumentOutOfRangeException(nameof(_facility));

        int loc = perm.GetParameterWithType(_facility);
        return loc != _location;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapForbiddenAssignmentConstraint expects a QapSolution", nameof(solution));

        int n = perm.Length;
        if (_facility < 0 || _facility >= n)
            throw new ArgumentOutOfRangeException(nameof(_facility));
        if (_location < 0 || _location >= n) return null;

        int currentLoc = perm.GetParameterWithType(_facility);
        if (currentLoc != _location) return solution; // already satisfied

        // find a facility j != _facility whose location is != _location (any candidate to swap)
        int swapWith = -1;
        for (int j = 0; j < n; ++j)
        {
            if (j == _facility) continue;
            int locJ = perm.GetParameterWithType(j);
            if (locJ != _location)
            {
                swapWith = j;
                break;
            }
        }

        if (swapWith == -1) return null; // cannot repair (unlikely for n>1)

        var repaired = (QapSolution)solution.Clone();
        int locJOld = perm.GetParameterWithType(swapWith);
        // swap them
        repaired.SetParameter(_facility, new Parameter(locJOld));
        repaired.SetParameter(swapWith, new Parameter(currentLoc));
        return IsSatisfied(repaired) ? repaired : null;
    }

    public string Description => $"QAP forbidden assignment: facility {_facility} != location {_location}";

    public string TextVisualization
    {
        get
        {
            return
                $@"QAP Forbidden Assignment Constraint
Facility: {_facility}
Forbidden location: {_location}

Description: This constraint prevents placing the facility at the forbidden location.
Repair behavior: If enabled, the facility is swapped with another facility to move it away from that location.";
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
}
