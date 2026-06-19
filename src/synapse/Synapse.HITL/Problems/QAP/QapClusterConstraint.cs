using System.Text;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;

namespace Synapse.HITL.Problems.QAP;

[DisplayInfo("QAP Facility Cluster Constraint",
    "Restricts a group of facilities so they may only be placed at specific allowed locations. "
    + "During repair, any facility in the group that is at a non-allowed location is swapped with a facility that occupies one of the allowed locations.")]
[ApplicableSolution(typeof(QapSolution))]
public class QapClusterConstraint : IConstraint
{
    private readonly int[] _facilities;
    private readonly HashSet<int> _allowedLocations;

    public QapClusterConstraint([DisplayInfo("Facilities")] IEnumerable<int> facilities, [DisplayInfo("Allowed Locations")] IEnumerable<int> allowedLocations)
    {
        if (facilities == null) throw new ArgumentNullException(nameof(facilities));
        if (allowedLocations == null) throw new ArgumentNullException(nameof(allowedLocations));
        _facilities = facilities as int[] ?? facilities.ToArray();
        _allowedLocations = new HashSet<int>(allowedLocations);
    }

    public bool IsSatisfied(ISolution solution)
    {
        if (solution is not PermutationSolution perm)
            throw new ArgumentException("QapClusterConstraint expects a PermutationSolution", nameof(solution));
        int n = perm.Length;
        ValidateFacilityIndices(_facilities, n);
        var assign = perm.GetParametersWithType(); // facility -> location
        foreach (var f in _facilities)
        {
            int loc = assign[f];
            if (!_allowedLocations.Contains(loc)) return false;
        }

        return true;
    }

    public ISolution? Repair(ISolution solution)
    {
        if (!RepairSolution) return solution;
        if (solution is not PermutationSolution perm)
            throw new ArgumentException("QapClusterConstraint expects a PermutationSolution", nameof(solution));
        int n = perm.Length;
        ValidateFacilityIndices(_facilities, n);

        // Quick feasibility: there must be at least as many allowed locations as facilities in cluster
        if (_allowedLocations.Count < _facilities.Length) return null;

        // Current assignment (facility -> location)
        int[] assign = perm.GetParametersWithType();

        // Build inverse mapping (location -> facility)
        int[] inverse = new int[n];
        for (int i = 0; i < n; ++i) inverse[i] = -1;
        for (int f = 0; f < n; ++f)
        {
            int loc = assign[f];
            if (loc < 0 || loc >= n)
                throw new InvalidOperationException("Solution is not a valid permutation (location out of range).");
            inverse[loc] = f;
        }

        // Determine which facilities are already OK and which need moving
        var assignedAllowed = new HashSet<int>();
        var needFix = new List<int>();
        foreach (var f in _facilities)
        {
            int loc = assign[f];
            if (_allowedLocations.Contains(loc))
                assignedAllowed.Add(loc);
            else
                needFix.Add(f);
        }

        if (needFix.Count == 0) return solution; // already satisfied

        // Free allowed locations (those allowed but not already occupied by cluster facilities)
        var freeAllowed = new Queue<int>(_allowedLocations.Except(assignedAllowed));

        // We'll build a new assignment array (deep copy) and modify it; avoid using solution.Clone()
        int[] newAssign = (int[])assign.Clone(); // facility -> location (ints are value types)
        int[] inverseNew = (int[])inverse.Clone();
        foreach (var f in needFix)
        {
            if (freeAllowed.Count == 0)
            {
                // Shouldn't happen because allowed.Count >= facilities.Count ensures enough free slots
                // But be defensive: try to find an allowed location occupied by a facility outside the cluster
                int fallbackLoc = -1;
                int fallbackOccupant = -1;
                foreach (var L in _allowedLocations)
                {
                    int occ = inverseNew[L];
                    if (occ != -1 && !_facilities.Contains(occ))
                    {
                        fallbackLoc = L;
                        fallbackOccupant = occ;
                        break;
                    }
                }

                if (fallbackLoc == -1) return null; // unrecoverable
                // perform swap: place f to fallbackLoc and occupant to old loc of f
                int oldLocF1 = newAssign[f];
                newAssign[f] = fallbackLoc;
                newAssign[fallbackOccupant] = oldLocF1;
                inverseNew[fallbackLoc] = f;
                inverseNew[oldLocF1] = fallbackOccupant;
                continue;
            }

            int chosenLoc = freeAllowed.Dequeue();
            int occupant = inverseNew[chosenLoc];
            int oldLocF = newAssign[f];
            if (occupant == -1)
            {
                // unlikely but handle: put f into chosenLoc and free oldLocF
                newAssign[f] = chosenLoc;
                inverseNew[chosenLoc] = f;
                inverseNew[oldLocF] = -1;
            }
            else
            {
                // swap occupant <-> f
                newAssign[f] = chosenLoc;
                newAssign[occupant] = oldLocF;
                inverseNew[chosenLoc] = f;
                inverseNew[oldLocF] = occupant;
            }
        }

        // Final sanity: newAssign must be a permutation
        if (newAssign.Length != n || newAssign.Distinct().Count() != n)
        {
            // If something still wrong, give up (or optionally perform a robust remap)
            return null;
        }

        // Build Parameter[] deep copy for new QapSolution
        var newParams = new Parameter[n];
        for (int f = 0; f < n; ++f) newParams[f] = new Parameter(newAssign[f]);
        var repaired = new QapSolution(newParams);
        return IsSatisfied(repaired) ? repaired : null;
    }

    public string Description =>
        $"QAP facility cluster: facilities [{string.Join(", ", _facilities.OrderBy(f => f))}] in allowed locations [{string.Join(", ", _allowedLocations.OrderBy(l => l))}]";

    public string TextVisualization
    {
        get
        {
            var facText = _facilities.Length == 0
                ? "none"
                : string.Join(", ", _facilities.OrderBy(x => x));
            var locText = _allowedLocations.Count == 0
                ? "none"
                : string.Join(", ", _allowedLocations.OrderBy(x => x));

            var sb = new StringBuilder();
            sb.AppendLine("QAP Cluster Constraint");
            sb.AppendLine($"Facilities: {facText}");
            sb.AppendLine($"Allowed locations: {locText}");
            sb.AppendLine();
            sb.AppendLine("Description: All listed facilities must be assigned within the allowed location set.");
            sb.AppendLine("Repair behavior: If enabled, out-of-set assignments are swapped into allowed locations.");
            return sb.ToString().TrimEnd();
        }
    }

    [DisplayInfo("Repair Solution")] public bool RepairSolution { get; set; } = false;

    // Helpers
    private static void ValidateFacilityIndices(IEnumerable<int> facilities, int n)
    {
        foreach (var f in facilities)
        {
            if (f < 0 || f >= n)
                throw new ArgumentOutOfRangeException(nameof(facilities),
                    $"Facility index {f} out of range [0..{n - 1}]");
        }
    }
}