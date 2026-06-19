using Synapse.OptimizationCore.Attributes;

namespace Synapse.Algorithms.GeneticAlgorithm.Crossover;

[DisplayInfo("Ordered Crossover")]
public class OrderedCrossover : ICrossover
{
    public Guid Id { get; } = Guid.NewGuid();
    public object Clone()
    {
        return new OrderedCrossover();
    }
}
