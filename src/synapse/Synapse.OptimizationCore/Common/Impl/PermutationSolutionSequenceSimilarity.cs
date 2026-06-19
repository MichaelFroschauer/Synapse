using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

[DisplayInfo(
    "Sequence Overlap Similarity",
    "Measures how similar two permutations are based on the length of their longest common subsequence, normalized by the shorter permutation."
)]
[ApplicableSolution(typeof(PermutationSolution))]
public class PermutationSolutionSequenceSimilarity : ISolutionSimilarity
{
    public double GetSimilarity(ISolution s1, ISolution s2)
    {
        if (s1 is not PermutationSolution ps1)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} is not a {nameof(PermutationSolution)}");
        if (s2 is not PermutationSolution ps2)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s2)} is not a {nameof(PermutationSolution)}");
        
        if (ps1.Length == 0 && ps2.Length == 0) return 1.0;
        if (ps1.Length == 0 || ps2.Length == 0) return 0.0;

        int[] psa1 = ps1.GetParametersWithType();
        int[] psa2 = ps2.GetParametersWithType();
        int lcs = PermutationSolutionSimilarity.LongestCommonSubsequenceLength(psa1, psa2);
        int minLen = Math.Min(psa1.Length, psa2.Length);
        return (double)lcs / minLen;
    }
}
