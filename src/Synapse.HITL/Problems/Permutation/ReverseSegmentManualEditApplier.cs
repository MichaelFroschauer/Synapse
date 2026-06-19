using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.HITL.Problems.Permutation;

[DisplayInfo("Reverse Segment Permutation Editor",
    "Reverses a segment of a permutation-based solution. The user provides two values that mark the start and end of the segment, and all elements between them (inclusive) are placed in reverse order.")]
[ApplicableSolution(typeof(PermutationSolution))]
public class ReverseSegmentByValuesManualEditApplier : IManualEditApplier
{
    public ISolution Apply(ISolution candidate, ISolution manual)
    {
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));
        if (manual == null) throw new ArgumentNullException(nameof(manual));

        Parameter[] mParams = manual.GetParameters();
        if (mParams == null || mParams.Length != 2)
            throw new ArgumentException("Manual must contain exactly 2 parameters (the two values that delimit the segment).", nameof(manual));

        Parameter valA = mParams[0];
        Parameter valB = mParams[1];
        
        ISolution result = candidate.Clone();
        Parameter[] arr = result.GetParameters() ?? throw new InvalidOperationException("Candidate parameters are null.");

        int idxA = -1, idxB = -1;
        for (int i = 0; i < arr.Length; i++)
        {
            if (idxA < 0 && arr[i].Equals(valA)) idxA = i;
            if (idxB < 0 && arr[i].Equals(valB)) idxB = i;
            if (idxA >= 0 && idxB >= 0) break;
        }

        if (idxA < 0)
            throw new ArgumentException($"Value '{valA}' from manual not found in candidate.", nameof(manual));
        if (idxB < 0)
            throw new ArgumentException($"Value '{valB}' from manual not found in candidate.", nameof(manual));

        // If same position -> no reversal necessary, but reset fitness
        if (idxA == idxB)
        {
            result.Fitness = null;
            return result;
        }

        int iStart = Math.Min(idxA, idxB);
        int iEnd   = Math.Max(idxA, idxB);

        // In-place reverse of the range [iStart..iEnd]
        while (iStart < iEnd)
        {
            (arr[iStart], arr[iEnd]) = (arr[iEnd], arr[iStart]);
            iStart++;
            iEnd--;
        }

        result.SetParameters(arr);
        result.Fitness = null;
        return result;
    }
}
