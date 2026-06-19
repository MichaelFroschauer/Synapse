using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;

namespace Synapse.HITL.Problems.QAP;

[DisplayInfo("QAP Permutation Sequence Editor",
    "Allows manual editing of a QAP facility-to-location assignment. "
    + "The user provides a new assignment sequence. Valid entries are applied directly, and any conflicts or missing assignments are resolved by filling in the remaining facilities.")]
[ApplicableSolution(typeof(QapSolution))]
public class QapManualEditApplier : IManualEditApplier
{

    public ISolution Apply(ISolution candidate, ISolution manual)
    {
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));
        if (manual == null) throw new ArgumentNullException(nameof(manual));
        if (candidate is not QapSolution basePerm)
            throw new ArgumentException("QapManualEditApplier expects a QapSolution candidate.", nameof(candidate));

        int n = basePerm.Length;
        // candidate assignment: facility -> location
        int[] baseAssign = basePerm.GetParametersWithType();

        // manual parameters may contain non-int values; null values are treated as missing entries.
        var manualParams = manual.GetParameters();
        if (manualParams.Length != n)
            throw new ArgumentException($"Manual solution must have exactly {n} parameters.", nameof(manual));

        // Build a dictionary of manual assignments: facility -> desiredLocation (only valid ints & in range)
        var manualAssignments = new Dictionary<int, int>();
        for (int f = 0; f < n; f++)
        {
            var p = manualParams[f];
            if (p.Value == null)
            {
                continue;
            }

            if (p.Value is int loc && loc >= 0 && loc < n)
            {
                manualAssignments[f] = loc;
            }
            // invalid value -> treat as missing
        }

        // If manual provided a complete (every facility) mapping (maybe with duplicates),
        // we apply "first occurrence wins" per location and then fill missing facilities with remaining locations.
        if (manualAssignments.Count == n)
        {
            // process facilities in ascending index: accept first facility that claims a location
            var usedLocations = new bool[n];
            var result = Enumerable.Repeat(-1, n).ToArray(); // facility -> location
            var unassignedFacilities = new List<int>();
            for (int f = 0; f < n; f++)
            {
                int desiredLoc = manualAssignments[f];
                if (!usedLocations[desiredLoc])
                {
                    result[f] = desiredLoc;
                    usedLocations[desiredLoc] = true;
                }
                else
                {
                    unassignedFacilities.Add(f); // duplicate target -> postpone assignment
                }
            }

            // collect remaining free locations
            var freeLocations = new Queue<int>(Enumerable.Range(0, n).Where(loc => !usedLocations[loc]));

            // assign free locations to unassigned facilities (in facility index order)
            foreach (var f in unassignedFacilities)
            {
                if (freeLocations.Count == 0)
                    throw new InvalidOperationException(
                        "No free locations available while repairing full manual mapping.");
                result[f] = freeLocations.Dequeue();
            }

            var parameters = result.Select(loc => new Parameter(loc)).ToArray();
            return new QapSolution(parameters);
        }

        // Partial or mixed manual suggestion:
        // We'll start from candidate assignment and apply each manual assignment in facility-index order.
        int[] resultAssign = (int[])baseAssign.Clone(); // facility -> location
        int[] inverse = new int[n]; // location -> facility
        for (int i = 0; i < n; i++) inverse[i] = -1;
        for (int f = 0; f < n; f++)
        {
            int loc = baseAssign[f];
            if (loc < 0 || loc >= n) throw new InvalidOperationException("Candidate is not a valid permutation.");
            inverse[loc] = f;
        }

        // Apply manual assignments in facility order (deterministic)
        foreach (var kv in manualAssignments.OrderBy(kv => kv.Key))
        {
            int f = kv.Key;
            int desiredLoc = kv.Value;
            int currentLoc = resultAssign[f];
            if (currentLoc == desiredLoc) continue; // nothing to do
            int occupant = inverse[desiredLoc];
            if (occupant == -1)
            {
                // defensive: if location unoccupied (shouldn't happen), simply place f there and free currentLoc
                resultAssign[f] = desiredLoc;
                inverse[desiredLoc] = f;
                inverse[currentLoc] = -1;
            }
            else if (occupant == f)
            {
                // already occupant (should have been caught by currentLoc check)
                continue;
            }
            else
            {
                // perform swap: occupant -> currentLoc, f -> desiredLoc
                resultAssign[occupant] = currentLoc;
                resultAssign[f] = desiredLoc;
                inverse[currentLoc] = occupant;
                inverse[desiredLoc] = f;
            }
        }

        // final sanity check: ensure resultAssign is a permutation
        if (resultAssign.Length != n || resultAssign.Distinct().Count() != n)
        {
            // as last resort, build a valid permutation: keep manual assignments where possible, then fill missing
            var usedLocs = new bool[n];
            var outAssign = Enumerable.Repeat(-1, n).ToArray();
            var unassignedFacs = new List<int>();
            for (int f = 0; f < n; f++)
            {
                if (manualAssignments.TryGetValue(f, out int desiredLoc) && !usedLocs[desiredLoc])
                {
                    outAssign[f] = desiredLoc;
                    usedLocs[desiredLoc] = true;
                }
                else
                {
                    unassignedFacs.Add(f);
                }
            }

            var freeLocs = new Queue<int>(Enumerable.Range(0, n).Where(loc => !usedLocs[loc]));
            foreach (var f in unassignedFacs)
            {
                outAssign[f] = freeLocs.Dequeue();
            }

            resultAssign = outAssign;
        }

        var finalParams = resultAssign.Select(loc => new Parameter(loc)).ToArray();
        return new QapSolution(finalParams);
    }
}
