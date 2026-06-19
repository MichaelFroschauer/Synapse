using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.JSP;

[DisplayInfo(
    "Position Match Similarity",
    "Computes similarity as the fraction of operations scheduled at the same position in both job shop solutions."
)]
[ApplicableSolution(typeof(JspSolution))]
public class JspSolutionSimilarity : ISolutionSimilarity
{
    public double GetSimilarity(ISolution s1, ISolution s2)
    {
        if (s1 is not JspSolution jsp1) 
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} is not a {nameof(JspSolution)}");
        if (s2 is not JspSolution jsp2) 
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s2)} is not a {nameof(JspSolution)}");
        if (jsp1.Length != jsp2.Length) 
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} and parameter {nameof(s2)} have different lengths.");
            
        if (jsp1.Length == 0 && jsp2.Length == 0) return 1.0;
        if (jsp1.Length == 0 || jsp2.Length == 0) return 0.0;

        var p1 = jsp1.GetParametersWithType();
        var p2 = jsp2.GetParametersWithType();
        var n = p1.Length;
        
        int similarItem = 0;
        for (int i = 0; i < n; i++)
        {
            if (p1[i] == p2[i]) similarItem++;
        }
        
        return similarItem / (double)n;
    }
}
