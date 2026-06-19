using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common;

/// <summary>
/// Computes population diversity using an <see cref="ISolutionSimilarity"/> measure.
/// Diversity is defined as the average pairwise dissimilarity (1 − similarity) over
/// a sample of the population.  When the population is large, a random sub-sample is
/// used so the cost stays roughly O(SampleSize²) instead of O(N²).
/// </summary>
public static class PopulationDiversityCalculator
{
    /// <summary>
    /// Default maximum number of solutions to sample when computing diversity.
    /// If the population is smaller, all pairs are used.
    /// </summary>
    public const int DefaultMaxSampleSize = 30;

    /// <summary>
    /// Computes the diversity of a population.
    /// Returns a value in [0, 1] where 0 = all identical, 1 = maximally diverse.
    /// </summary>
    /// <param name="population">The full population of solutions.</param>
    /// <param name="similarityMeasure">
    /// The similarity measure to use. If null, the default similarity of the first
    /// solution (<see cref="ISolution.GetDefaultSolutionSimilarityClass"/>) is used.
    /// </param>
    /// <param name="maxSampleSize">
    /// Maximum number of solutions to include in the pairwise comparison.
    /// Pass 0 or negative to use all solutions (warning: O(N²)).
    /// </param>
    /// <param name="seed">Optional RNG seed for reproducible sampling.</param>
    /// <returns>Average pairwise dissimilarity in [0, 1], or <c>null</c> if the
    /// population has fewer than 2 solutions or no similarity measure is available.</returns>
    public static double? ComputeDiversity(
        IReadOnlyList<ISolution> population,
        ISolutionSimilarity? similarityMeasure = null,
        int maxSampleSize = DefaultMaxSampleSize,
        int? seed = null)
    {
        if (population.Count < 2) return null;

        // Resolve similarity measure
        similarityMeasure ??= TryGetSimilarity(population);
        if (similarityMeasure is null) return null;

        // Sample if necessary
        IReadOnlyList<ISolution> sample = population;
        if (maxSampleSize > 0 && population.Count > maxSampleSize)
        {
            sample = RandomSample(population, maxSampleSize, seed);
        }

        // Compute average pairwise dissimilarity
        int n = sample.Count;
        if (n < 2) return null;

        double totalDissimilarity = 0.0;
        int pairCount = 0;

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                try
                {
                    double sim = similarityMeasure.GetSimilarity(sample[i], sample[j]);
                    // Clamp to [0,1] just in case an implementation is slightly off
                    sim = Math.Clamp(sim, 0.0, 1.0);
                    totalDissimilarity += 1.0 - sim;
                    pairCount++;
                }
                catch
                {
                    // Skip pairs that fail (e.g. incompatible solution types)
                }
            }
        }

        if (pairCount == 0) return null;
        return totalDissimilarity / pairCount;
    }

    /// <summary>
    /// Tries to obtain a similarity measure from the first solution in the population.
    /// </summary>
    private static ISolutionSimilarity? TryGetSimilarity(IReadOnlyList<ISolution> population)
    {
        try
        {
            return population[0].GetDefaultSolutionSimilarityClass();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fisher-Yates partial shuffle to pick <paramref name="count"/> random elements.
    /// </summary>
    private static IReadOnlyList<ISolution> RandomSample(
        IReadOnlyList<ISolution> population,
        int count,
        int? seed)
    {
        int n = population.Count;
        count = Math.Min(count, n);

        // Copy indices, then partially shuffle
        var indices = new int[n];
        for (int i = 0; i < n; i++) indices[i] = i;

        var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        for (int i = 0; i < count; i++)
        {
            int j = rng.Next(i, n);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var result = new ISolution[count];
        for (int i = 0; i < count; i++)
            result[i] = population[indices[i]];

        return result;
    }
}
