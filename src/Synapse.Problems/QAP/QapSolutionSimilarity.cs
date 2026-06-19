using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.QAP;

[DisplayInfo(
    "Assignment Match Similarity",
    "Measures similarity by the proportion of facilities assigned to the same locations in both solutions."
)]
[ApplicableSolution(typeof(QapSolution))]
public class QapSolutionSimilarity : ISolutionSimilarity
{
    public double GetSimilarity(ISolution s1, ISolution s2)
    {
        if (s1 is not QapSolution ps1)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} is not a {nameof(QapSolution)}");
        if (s2 is not QapSolution ps2)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s2)} is not a {nameof(QapSolution)}");
        if (s1.Length != s2.Length) throw new ArgumentException("Permutation lengths must be equal for QAP.");

        int n = s1.Length;
        if (n == 0) return 1.0;

        var psa1 = ps1.GetParametersWithType();
        var psa2 = ps2.GetParametersWithType();
        
        int same = 0;
        for (int i = 0; i < n; i++)
        {
            if (psa1[i] == psa2[i]) same++;
        }

        return (double)same / n;
    }
}
