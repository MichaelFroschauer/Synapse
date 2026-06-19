using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;

namespace Synapse.Algorithms.ParticleSwarm;

/// <summary>
/// Configuration for the Particle Swarm Optimization algorithm.
/// </summary>
public class ParticleSwarmConfig : BaseAlgorithmConfig
{
    public override AlgorithmType AlgorithmType => AlgorithmType.ParticleSwarm;
    
    public int SwarmSize { get; set; } = 50;
    public double Inertia { get; set; } = 0.729;
    public double Cognitive { get; set; } = 1.49445;
    public double Social { get; set; } = 1.49445;
    
    /// <summary>Fraction of (max-min) to clamp velocities.</summary>
    public double VelocityClampFactor { get; set; } = 0.5;

    /// <summary>Lower bounds for each dimension (null = use problem defaults).</summary>
    public double[]? PositionMin { get; set; }
    
    /// <summary>Upper bounds for each dimension (null = use problem defaults).</summary>
    public double[]? PositionMax { get; set; }

    public ParticleSwarmConfig() { }
    
    private ParticleSwarmConfig(ParticleSwarmConfig config) : base(config)
    {
        SwarmSize = config.SwarmSize;
        Inertia = config.Inertia;
        Cognitive = config.Cognitive;
        Social = config.Social;
        VelocityClampFactor = config.VelocityClampFactor;
        PositionMin = config.PositionMin?.Clone() as double[];
        PositionMax = config.PositionMax?.Clone() as double[];
    }
    
    public override object Clone() => new ParticleSwarmConfig(this);
}
