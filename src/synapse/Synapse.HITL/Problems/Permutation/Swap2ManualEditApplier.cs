using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.HITL.Problems.Permutation;

[DisplayInfo("Swap2 Permutation Editor",
    "Swaps two values in a permutation-based solution. The user provides exactly two values, and their positions in the solution are exchanged.")]
[ApplicableSolution(typeof(PermutationSolution))]
public class Swap2ManualEditApplier : IManualEditApplier
{
    public ISolution Apply(ISolution candidate, ISolution manual)
    {
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));
        if (manual == null) throw new ArgumentNullException(nameof(manual));

        Parameter[] manualParams = manual.GetParameters();
        if (manualParams == null || manualParams.Length != 2)
            throw new ArgumentException("Manual solution must contain exactly 2 parameters (the two values to swap).", nameof(manual));

        // solution values to swap
        Parameter valueA = manualParams[0];
        Parameter valueB = manualParams[1];
        
        ISolution result = candidate.Clone();
        Parameter[] resultParams = result.GetParameters();
        if (resultParams == null) throw new InvalidOperationException("Candidate parameters are null.");

        int indexA = -1, indexB = -1;
        for (int i = 0; i < resultParams.Length; i++)
        {
            if (indexA < 0 && resultParams[i].Equals(valueA)) indexA = i;
            if (indexB < 0 && resultParams[i].Equals(valueB)) indexB = i;

            // break if both indices are found
            if (indexA >= 0 && indexB >= 0) break;
        }

        // If the values have not been found an exception in thrown because the manual solution contains values that are not in the solution candidate
        if (indexA < 0)
            throw new ArgumentException($"Value '{valueA}' from manual solution not found in candidate.", nameof(manual));
        if (indexB < 0)
            throw new ArgumentException($"Value '{valueB}' from manual solution not found in candidate.", nameof(manual));

        // If both indices are the manual solution is still false, but we don't care in this case
        if (indexA == indexB)
        {
            result.Fitness = null;
            return result;
        }

        // swap these params
        (resultParams[indexA], resultParams[indexB]) = (resultParams[indexB], resultParams[indexA]);

        result.SetParameters(resultParams);

        // reset fitness because the solution changed
        result.Fitness = null;

        return result;
    }
}
