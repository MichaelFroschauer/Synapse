using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.Algorithms.ParticleSwarm.Mapper;

public abstract class BaseParticleMapper : IParticleMapper
{
    protected static IRandom Random => RandomProvider.Value;

    public abstract int Dimensions { get; }
    public virtual (double[] Min, double[] Max)? Bounds => null;

    public abstract ISolution PositionToSolution(double[] position);
    
    public virtual double[] CreateRandomPosition()
    {
        var pos = new double[Dimensions];
        //for (int i = 0; i < Dimensions; i++) pos[i] = Random.GetDouble() * 5 * (Random.GetDouble() > 0.5 ? -1 : 1);
        for (int i = 0; i < Dimensions; i++) pos[i] = Random.GetDouble();
        return pos;
    }
}
