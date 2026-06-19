using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

[DisplayInfo(
    "Euclidean Distance Similarity",
    "Measures similarity by converting the Euclidean distance between two real-valued vectors into a normalized similarity score."
)]
[ApplicableSolution(typeof(RealValueSolution))]
public class RealValueSolutionEuclideanSimilarity : ISolutionSimilarity
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
        
        /* Calculate distance similarity */
        // double sum = 0.0;
        //
        // for (int i = 0; i < a.Length; i++)
        // {
        //     double diff = a[i] - b[i];
        //     sum += diff * diff;
        // }
        //
        // double dist = Math.Sqrt(sum);
        // double maxDist = Math.Sqrt(a.Length);
        //
        // // map to [0,1]
        // double sim = 1.0 - (dist / maxDist);
        //
        // if (sim < 0.0) return 0.0;
        // if (sim > 1.0) return 1.0;
        //
        // return sim;

        var scaling = 1.0;
        var distance = 0.0;
        for (int i = 0; i < a.Length; i++)
        {
            distance += (a[i] - b[i]) * (a[i] - b[i]);
        }
        
        return 1.0 / (1.0 + Math.Sqrt(distance) / scaling);
    }
}
