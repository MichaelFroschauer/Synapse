using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Algorithms.ParticleSwarm.Mapper;

/// <summary>
/// Mapper abstraction between double[] positions and domain ISolution.
/// Implementations must ensure that PositionToSolution produces a valid ISolution.
/// </summary>
public interface IParticleMapper
{
    /// <summary>Transform a position vector into a domain solution.</summary>
    ISolution PositionToSolution(double[] position);

    /// <summary>Optionally create an initial random position; if not implemented, PSO will rely on the config.CreateRandomPosition delegate.</summary>
    double[]? CreateRandomPosition();

    /// <summary>Dimensions of the position vector the mapper expects.</summary>
    int Dimensions { get; }

    /// <summary>Optional min/max bounds for each dimension. If null PSO will not enforce per-dimension bounds.</summary>
    (double[] Min, double[] Max)? Bounds { get; }
}