using System.Text.Json.Serialization;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;

namespace Synapse.OptimizationCore.Interfaces;

/// <summary>
/// Configuration contract for all metaheuristic algorithms.
/// Defines common parameters, controller references, and cloning support.
/// </summary>
public interface IAlgorithmConfig : ICloneable
{
    /// <summary>Optional human-readable name for this configuration / job.</summary>
    string Name { get; set; }
    
    /// <summary>Maximum wall-clock seconds before the algorithm is stopped (null = no limit).</summary>
    int? TimeoutSeconds { get; set; }
    
    /// <summary>How often (in iterations) to publish progress events to subscribers.</summary>
    int ProgressInterval { get; set; }
    
    /// <summary>Maximum number of iterations before the algorithm terminates.</summary>
    int MaxIterations { get; set; }
    
    /// <summary>
    /// External controller for pause/resume/stop and progress observation.
    /// Assigned by the <see cref="Synapse.JobManagement.JobManager"/> before execution starts.
    /// </summary>
    [JsonIgnore]
    IAlgorithmController? AlgorithmController { get; set; }
    
    /// <summary>
    /// Human-in-the-loop controller for constraints, preferences, manual edits, and scripts.
    /// Assigned by the <see cref="Synapse.JobManagement.JobManager"/> before execution starts.
    /// </summary>
    [JsonIgnore]
    IHitlController? HitlController { get; set; }
    
    /// <summary>Identifies which algorithm type this configuration belongs to.</summary>
    AlgorithmType AlgorithmType { get; }
    
    /// <summary>Optional random seed for reproducibility (null = non-deterministic).</summary>
    int? Seed { get; set; }
    
    /// <summary>
    /// Algorithm-level similarity measure for internal use (diversity, duplicate detection, etc.).
    /// Separate from the HITL controller's similarity measure used for user preference choices.
    /// </summary>
    ISolutionSimilarity? SimilarityMeasure { get; set; }
    
    /// <summary>
    /// Returns the configured algorithm similarity measure, falling back to the solution's default.
    /// </summary>
    ISolutionSimilarity GetAlgorithmSimilarity(ISolution solution);
}
