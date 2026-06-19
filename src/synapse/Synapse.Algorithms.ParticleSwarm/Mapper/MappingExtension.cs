using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Algorithms.ParticleSwarm.Mapper;

public static class MappingExtension
{
    public static IParticleMapper GetMapper(this ISolution solution)
    {
        if (solution == null) throw new ArgumentNullException(nameof(solution));
        
        return solution switch
        {
            PermutationSolution ps => new PermutationParticleMapper(ps),
            RealValueSolution rs => new RealValueParticleMapper(rs),
            _ => throw new NotSupportedException($"Unsupported solution type: {solution.GetType().FullName}")
        };
    }
}
