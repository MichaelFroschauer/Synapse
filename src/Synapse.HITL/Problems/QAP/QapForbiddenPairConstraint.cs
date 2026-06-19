using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;

namespace Synapse.HITL.Problems.QAP;

[DisplayInfo("QAP Forbidden Facility Pair Constraint",
    "Prevents two specific facilities from being placed at adjacent locations. "
    + "The constraint is violated if the location indices of the two facilities differ by at most 1. "
    + "During repair, one facility is swapped to a non-adjacent location.")]
[ApplicableSolution(typeof(QapSolution))]
public class QapForbiddenPairConstraint : IConstraint
{
    private readonly int _facilityA;
    private readonly int _facilityB;

    public QapForbiddenPairConstraint([DisplayInfo("Facility A")] int facilityA, [DisplayInfo("Facility B")] int facilityB)
    {
        _facilityA = facilityA;
        _facilityB = facilityB;
    }
    
    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapForbiddenPairConstraint expects a QapSolution", nameof(solution));

        if (_facilityA < 0 || _facilityA >= perm.Length)
            throw new ArgumentOutOfRangeException(nameof(_facilityA));
        if (_facilityB < 0 || _facilityB >= perm.Length)
            throw new ArgumentOutOfRangeException(nameof(_facilityB));

        int a = perm.GetParameterWithType(_facilityA);
        int b = perm.GetParameterWithType(_facilityB);
        return Math.Abs(a - b) > 1;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapForbiddenPairConstraint expects a QapSolution", nameof(solution));

        int n = perm.Length;
        if (_facilityA < 0 || _facilityA >= n)
            throw new ArgumentOutOfRangeException(nameof(_facilityA));
        if (_facilityB < 0 || _facilityB >= n)
            throw new ArgumentOutOfRangeException(nameof(_facilityB));

        int aLoc = perm.GetParameterWithType(_facilityA);
        int bLoc = perm.GetParameterWithType(_facilityB);
        if (Math.Abs(aLoc - bLoc) > 1) return solution; // already satisfied

        // try swapping facilityA with some facility j that makes it non-adjacent to facilityB
        for (int j = 0; j < n; ++j)
        {
            if (j == _facilityA || j == _facilityB) continue;
            int jLoc = perm.GetParameterWithType(j);
            if (Math.Abs(jLoc - bLoc) > 1)
            {
                var repaired = (QapSolution)solution.Clone();
                // swap A <-> j
                repaired.SetParameter(_facilityA, new Parameter(jLoc));
                repaired.SetParameter(j, new Parameter(aLoc));
                return IsSatisfied(repaired) ? repaired : null;
            }
        }

        // try swapping facilityB instead
        for (int j = 0; j < n; ++j)
        {
            if (j == _facilityA || j == _facilityB) continue;
            int jLoc = perm.GetParameterWithType(j);
            if (Math.Abs(aLoc - jLoc) > 1)
            {
                var repaired = (QapSolution)solution.Clone();
                repaired.SetParameter(_facilityB, new Parameter(jLoc));
                repaired.SetParameter(j, new Parameter(bLoc));
                return IsSatisfied(repaired) ? repaired : null;
            }
        }

        // no simple local swap found
        return null;
    }

    public string Description => $"QAP forbidden pair proximity: facilities {_facilityA} and {_facilityB}";

    public string TextVisualization
    {
        get
        {
            return
                $@"QAP Forbidden Pair Constraint
Facility A: {_facilityA}
Facility B: {_facilityB}

Description: These two facilities may not be placed in adjacent locations.
Repair behavior: If enabled, one of the facilities is swapped to break adjacency.";
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;
}