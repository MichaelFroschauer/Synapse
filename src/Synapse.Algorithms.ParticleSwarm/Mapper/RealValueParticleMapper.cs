using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.Function;

namespace Synapse.Algorithms.ParticleSwarm.Mapper;

public class RealValueParticleMapper : BaseParticleMapper
{
    private readonly RealValueSolution _prototype;
    public override int Dimensions { get; }
    
    public RealValueParticleMapper(RealValueSolution prototype)
    {
        _prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));
        Dimensions = _prototype.Length;
    }

    public override ISolution PositionToSolution(double[] position)
    {
        if (position.Length != Dimensions) throw new ArgumentException("Position length mismatch.", nameof(position)); 
        return _prototype.Create(position);
    }
}
