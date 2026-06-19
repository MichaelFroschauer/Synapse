using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

[DisplayInfo(
    "Normalized LCS Similarity",
    "Compares two permutations using the longest common subsequence length, normalized by the combined lengths of both permutations."
)]
[ApplicableSolution(typeof(PermutationSolution))]
public class PermutationSolutionSimilarity : ISolutionSimilarity
{
    public virtual double GetSimilarity(ISolution s1, ISolution s2)
    {
        if (s1 is not PermutationSolution ps1)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} is not a {nameof(PermutationSolution)}");
        if (s2 is not PermutationSolution ps2)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s2)} is not a {nameof(PermutationSolution)}");
        
        if (ps1.Length == 0 && ps2.Length == 0) return 1.0;
        if (ps1.Length == 0 || ps2.Length == 0) return 0.0;

        int[] psa1 = ps1.GetParametersWithType();
        int[] psa2 = ps2.GetParametersWithType();
        int lcs = LongestCommonSubsequenceLength(psa1, psa2);

        // 2 * LCS / (lenA + lenB) in [0,1]
        return (2.0 * lcs) / (ps1.Length + ps2.Length);
    }

    public static int LongestCommonSubsequenceLength(int[] psa1, int[] psa2)
    {
        int n = psa1.Length;
        int m = psa2.Length;
        int[,] dp = new int[n + 1, m + 1];

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                if (psa1[i - 1].Equals(psa2[j - 1]))
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        return dp[n, m];
    }
}
