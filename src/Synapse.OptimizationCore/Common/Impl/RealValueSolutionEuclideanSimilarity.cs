using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

[DisplayInfo(
    "Normalized Euclidean Similarity",
    "Measures normalized Euclidean similarity between real-valued solutions."
)]
[ApplicableSolution(typeof(RealValueSolution))]
public class RealValueSolutionEuclideanSimilarity : ISolutionSimilarity
{
    private readonly double _minimum;
    private readonly double _maximum;

    public RealValueSolutionEuclideanSimilarity(
        double minimum = -5.12,
        double maximum = 5.12)
    {
        if (maximum <= minimum)
            throw new ArgumentException(
                "Maximum must be greater than minimum.");

        _minimum = minimum;
        _maximum = maximum;
    }

    public double GetSimilarity(ISolution s1, ISolution s2)
    {
        if (s1 is not RealValueSolution rv1)
        {
            throw new ArgumentException(
                $"{nameof(s1)} must be a {nameof(RealValueSolution)}.");
        }

        if (s2 is not RealValueSolution rv2)
        {
            throw new ArgumentException(
                $"{nameof(s2)} must be a {nameof(RealValueSolution)}.");
        }

        if (rv1.Length != rv2.Length)
        {
            throw new ArgumentException(
                "The solutions must have the same number of dimensions.");
        }

        if (rv1.Length == 0)
            return 1.0;

        var a = rv1.GetParametersWithType();
        var b = rv2.GetParametersWithType();

        double range = _maximum - _minimum;
        double squaredNormalizedDistance = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            double normalizedDifference = (a[i] - b[i]) / range;
            squaredNormalizedDistance +=
                normalizedDifference * normalizedDifference;
        }

        // Root mean squared normalized distance.
        // Result is in [0, 1] when all values are inside the bounds.
        double normalizedDistance =
            Math.Sqrt(squaredNormalizedDistance / a.Length);

        return Math.Clamp(1.0 - normalizedDistance, 0.0, 1.0);
    }
}
