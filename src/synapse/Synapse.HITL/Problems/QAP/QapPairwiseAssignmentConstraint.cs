using System.Text;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;

namespace Synapse.HITL.Problems.QAP;

[DisplayInfo("QAP Pairwise Facility Assignment Constraint",
    "Ensures that two facilities are assigned to locations that are within a given maximum distance of each other. "
    + "The distance is measured using the problem's distance matrix. "
    + "During repair, one facility is moved to a closer location by swapping with another facility.")]
[ApplicableSolution(typeof(QapSolution))]
public class QapPairwiseAssignmentConstraint : IConstraint
{
    private readonly int _facilityA;
    private readonly int _facilityB;
    private readonly double _maxDistance;
    private readonly int[,]? _D;

    public QapPairwiseAssignmentConstraint(QapProblem problem, [DisplayInfo("Facility A")] int facilityA, [DisplayInfo("Facility B")] int facilityB, double maxDistance)
    {
        _facilityA = facilityA;
        _facilityB = facilityB;
        _maxDistance = maxDistance;
        _D = problem.DistanceMatrix;
    }

    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapPairwiseAssignmentConstraint expects a QapSolution",
                nameof(solution));
        int n = perm.Length;
        ValidateFacilityIndex(_facilityA, n);
        ValidateFacilityIndex(_facilityB, n);
        int locA = perm.GetParameterWithType(_facilityA);
        int locB = perm.GetParameterWithType(_facilityB);
        return Distance(locA, locB) <= _maxDistance;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution is not QapSolution perm)
            throw new ArgumentException("QapPairwiseAssignmentConstraint expects a QapSolution",
                nameof(solution));
        int n = perm.Length;
        ValidateFacilityIndex(_facilityA, n);
        ValidateFacilityIndex(_facilityB, n);
        int locA = perm.GetParameterWithType(_facilityA);
        int locB = perm.GetParameterWithType(_facilityB);
        if (Distance(locA, locB) <= _maxDistance) return solution; // already satisfied

        // Try swapping facility A with some j so that new locA (== loc_j) is within distance to locB
        for (int j = 0; j < n; ++j)
        {
            if (j == _facilityA || j == _facilityB) continue;
            int locJ = perm.GetParameterWithType(j);
            if (Distance(locJ, locB) <= _maxDistance)
            {
                var repaired = (QapSolution)solution.Clone();
                // swap A <-> j
                repaired.SetParameter(_facilityA, new Parameter(locJ));
                repaired.SetParameter(j, new Parameter(locA));
                return IsSatisfied(repaired) ? repaired : null;
            }
        }

        // Try swapping facility B with some j so that new locB (== loc_j) is within distance to locA
        for (int j = 0; j < n; ++j)
        {
            if (j == _facilityA || j == _facilityB) continue;
            int locJ = perm.GetParameterWithType(j);
            if (Distance(locA, locJ) <= _maxDistance)
            {
                var repaired = (QapSolution)solution.Clone();
                // swap B <-> j
                repaired.SetParameter(_facilityB, new Parameter(locJ));
                repaired.SetParameter(j, new Parameter(locB));
                return IsSatisfied(repaired) ? repaired : null;
            }
        }

        // no simple single-swap repair found
        return null;
    }

    public string Description =>
        $"QAP pairwise distance: facilities {_facilityA} & {_facilityB} within {_maxDistance}";

    public string TextVisualization
    {
        get
        {
            var usesMatrix = _D != null ? "yes" : "no";
            var maxDistText = _maxDistance.ToString("G");

            var sb = new StringBuilder();
            sb.AppendLine("Pairwise Assignment Constraint");
            sb.AppendLine($"Facility A: {_facilityA}");
            sb.AppendLine($"Facility B: {_facilityB}");
            sb.AppendLine($"Maximum distance: {maxDistText}");
            sb.AppendLine();
            sb.AppendLine("Description: The locations assigned to both facilities must be within the specified distance.");
            sb.AppendLine($"Distance matrix available: {usesMatrix}");
            sb.AppendLine("Repair behavior: If enabled, a swap is attempted to move one facility closer.");
            return sb.ToString().TrimEnd();
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;

    private static void ValidateFacilityIndex(int facility, int n)
    {
        if (facility < 0 || facility >= n)
            throw new ArgumentOutOfRangeException(nameof(facility),
                $"Facility index {facility} out of range [0..{n - 1}]");
    }

    // helper: compute distance using DistanceMatrix if present, else index difference
    private double Distance(int locA, int locB)
    {
        if (_D != null)
        {
            if (locA < 0 || locA >= _D.GetLength(0) || locB < 0 || locB >= _D.GetLength(1))
                throw new ArgumentOutOfRangeException("Location indices out of range for provided DistanceMatrix matrix.");
            return _D[locA, locB];
        }

        return Math.Abs(locA - locB);
    }
}