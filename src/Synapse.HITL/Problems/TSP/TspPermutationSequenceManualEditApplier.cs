using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;

namespace Synapse.HITL.Problems.TSP;

[DisplayInfo("TSP Permutation Sequence Editor",
    "Allows manual editing of a TSP tour by providing a new city sequence. "
    + "Valid manual cities are deduplicated and inserted as one ordered block near their first matching position in the original tour, the remaining cities keep their original relative order. "
    + "Finally, the tour is rotated so the original starting city stays at index 0.")]
[ApplicableSolution(typeof(TspSolution))]
public class TspPermutationSequenceManualEditApplier : IManualEditApplier
{
    public ISolution Apply(ISolution candidate, ISolution manual)
    {
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));
        if (manual == null) throw new ArgumentNullException(nameof(manual));
        if (candidate is not TspSolution s)
            throw new ArgumentException("TspPermutationSequenceManualEditApplier expects a TspSolution candidate.", nameof(candidate));
        if (manual is not TspSolution m)
            throw new ArgumentException("TspPermutationSequenceManualEditApplier expects a TspSolution manual solution.", nameof(manual));

        var baseTour = s.GetParametersWithType();
        var manualTour = m.GetParametersWithType();

        int n = baseTour.Length;
        if (n == 0) return new TspSolution(baseTour);

        // Valid cities (from candidate), only these may be used
        var validCities = new HashSet<int>(baseTour);

        // Filter manual sequence: only valid cities, no duplicates (keep first occurrence)
        var manualSeq = new List<int>();
        var seen = new HashSet<int>();
        foreach (var c in manualTour)
        {
            if (!validCities.Contains(c)) continue;      // ignore invalid city
            if (seen.Add(c)) manualSeq.Add(c);           // retain first occurrence
        }

        if (manualSeq.Count == 0)
        {
            // nothing to do, return copy / new solution
            return new TspSolution(baseTour.ToArray());
        }

        // Remaining cities in the order of baseTour (excluding manual cities)
        var remaining = baseTour.Where(c => !manualSeq.Contains(c)).ToList();

        // Determine insertion position in the original baseTour: first occurrence of any manual city
        int insertAnchorInBase = -1;
        for (int i = 0; i < baseTour.Length; i++)
        {
            if (manualSeq.Contains(baseTour[i]))
            {
                insertAnchorInBase = i;
                break;
            }
        }
        if (insertAnchorInBase == -1)
        {
            // no match, insert at the beginning
            insertAnchorInBase = 0;
        }

        // Calculate insertion position within 'remaining'
        //    Number of elements in baseTour before insertAnchorInBase that are not in manualSeq
        int remInsertIdx = 0;
        for (int i = 0; i < insertAnchorInBase; i++)
        {
            if (!manualSeq.Contains(baseTour[i])) remInsertIdx++;
        }
        if (remInsertIdx < 0) remInsertIdx = 0;
        if (remInsertIdx > remaining.Count) remInsertIdx = remaining.Count;

        // Build result: remaining[0..remInsertIdx-1] + manualSeq + remaining[remInsertIdx..end]
        var result = new List<int>(n);
        result.AddRange(remaining.Take(remInsertIdx));
        result.AddRange(manualSeq);
        result.AddRange(remaining.Skip(remInsertIdx));

        // Sanity check: if for some reason not all cities are present, restore permutation
        if (result.Count != n || result.Distinct().Count() != n)
        {
            // Fallback: Merge as before, take baseTour, add manual order before (replace occurrences)
            var fallback = new List<int>();
            var used = new HashSet<int>();
            // First all manual ones (in order), if valid
            foreach (var c in manualSeq)
            {
                if (used.Add(c)) fallback.Add(c);
            }
            // then all from baseTour that are still missing (in their order)
            foreach (var c in baseTour)
            {
                if (used.Add(c)) fallback.Add(c);
            }
            result = fallback;
        }

        // Rotate so that the original starting city of the candidate tour remains at index 0
        int candidateStart = baseTour[0];
        int idx = result.IndexOf(candidateStart);
        if (idx > 0)
        {
            var rotated = result.Skip(idx).Concat(result.Take(idx)).ToArray();
            return new TspSolution(rotated);
        }

        return new TspSolution(result.ToArray());
    }
}