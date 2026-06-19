using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

[DisplayInfo(
    "Cosine Similarity",
    "Measures similarity between two real-valued vectors based on their orientation, independent of magnitude."
)]
[ApplicableSolution(typeof(RealValueSolution))]
public class RealValueSolutionCosineSimilarity : ISolutionSimilarity
{
    public double GetSimilarity(ISolution s1, ISolution s2)
    {
        if (s1 is not RealValueSolution rv1)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} is not a {nameof(RealValueSolution)}");
        if (s2 is not RealValueSolution rv2)
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s2)} is not a {nameof(RealValueSolution)}");
        if (rv1.Length != rv2.Length) 
            throw new ArgumentException($"{nameof(GetSimilarity)}: Parameter {nameof(s1)} and parameter {nameof(s2)} have different lengths.");

        
        if (rv1.Length == 0 && rv2.Length == 0) return 1.0;
        if (rv1.Length == 0 || rv2.Length == 0) return 0.0;

        var a = rv1.GetParametersWithType();
        var b = rv2.GetParametersWithType();
        
        
        /* Calculate cosine similarity */
        double dot = 0.0;
        double magA = 0.0;
        double magB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        double denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        if (denom == 0.0) return 0.0;

        double cosine = dot / denom;

        // map [-1,1] to [0,1]
        return (cosine + 1.0) / 2.0;
    }
}
