using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Crossover;

[DisplayInfo("One Point Crossover")]
public class OnePointCrossover : ICrossover
{
    public Guid Id { get; } = Guid.NewGuid();
    public object Clone()
    {
        return new OnePointCrossover();
    }
}
