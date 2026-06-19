using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

/// <summary>
/// Base implementation of <see cref="IAlgorithmConfig"/> with common algorithm parameters.
/// All concrete algorithm configs should derive from this class.
/// </summary>
public abstract class BaseAlgorithmConfig : IAlgorithmConfig
{
    /// <inheritdoc />
    public string Name { get; set; } = "";
    
    /// <inheritdoc />
    public int? TimeoutSeconds { get; set; }
    
    /// <inheritdoc />
    public int ProgressInterval { get; set; } = 10;
    
    /// <inheritdoc />
    public int MaxIterations { get; set; } = 1000;
    
    /// <inheritdoc />
    public int? Seed { get; set; }
    
    /// <inheritdoc />
    public ISolutionSimilarity? SimilarityMeasure { get; set; }
    
    /// <inheritdoc />
    public IAlgorithmController? AlgorithmController { get; set; }
    
    /// <inheritdoc />
    public IHitlController? HitlController { get; set; }
    
    /// <inheritdoc />
    public abstract AlgorithmType AlgorithmType { get; }
    
    /// <inheritdoc />
    public abstract object Clone();

    protected BaseAlgorithmConfig() {}
    
    // Copy constructor. Copies all value-type and reference-type base properties
    protected BaseAlgorithmConfig(BaseAlgorithmConfig config)
    {
        Name = config.Name;
        TimeoutSeconds = config.TimeoutSeconds;
        ProgressInterval = config.ProgressInterval;
        MaxIterations = config.MaxIterations;
        Seed = config.Seed;
        SimilarityMeasure = config.SimilarityMeasure;
    }
    
    /// <inheritdoc />
    public ISolutionSimilarity GetAlgorithmSimilarity(ISolution solution)
    {
        return SimilarityMeasure ?? solution.GetDefaultSolutionSimilarityClass();
    }
}
